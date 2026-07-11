using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class CreateSpaceHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    SyncPermissionService syncPermission,
    RealtimeService realtimeService,
    IdempotencyService idempotencyService,
    ILogger<CreateSpaceHandler> logger
) : ICommandHandler<CreateSpaceCommand, long>
{
    public async Task<Result<long>> Handle(CreateSpaceCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to create space '{SpaceName}' under Workspace {WorkspaceId}", request.Name, workspaceContext.WorkspaceId);
        syncPermission.RequireMember();

        var creatorId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        ProjectSpace? space = null;
        List<SyncEvent> events = [];

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            var hasProcessed = await idempotencyService.HasProcessedAsync(request.TraceId, cancellationToken);
            if (hasProcessed)
            {
                logger.LogInformation("Idempotent bypass for trace {TraceId}. Skipping.", request.TraceId);
                return Result<long>.Success(0);
            }

            var maxKey = await db.ProjectSpaces
                .AsNoTracking()
                .Where(s => s.ProjectWorkspaceId == workspaceContext.WorkspaceId && s.DeletedAt == null)
                .Select(s => (string?)s.OrderKey)
                .MaxAsync(cancellationToken);

            var orderKey = FractionalIndex.SafeAfter(maxKey);
            var slug = SlugHelper.GenerateSlug(request.Name);
            var jsonOptions = SyncJson.Options;

            space = ProjectSpace.Create(
                id: request.Id,
                projectWorkspaceId: workspaceContext.WorkspaceId,
                name: request.Name,
                slug: slug,
                defaultDocumentId: request.DefaultDocumentId,
                color: request.Color,
                icon: request.Icon,
                isPrivate: request.IsPrivate,
                creatorId: creatorId,
                orderKey: orderKey
            );
            db.ProjectSpaces.Add(space);
            events.Add(new SyncEvent
            {
                ProjectWorkspaceId = workspaceContext.WorkspaceId,
                EntityType = SyncEntityType.Space,
                EntityId = space.Id,
                Action = SyncAction.C,
                Payload = JsonSerializer.Serialize(new
                {
                    id = space.Id,
                    workspaceId = workspaceContext.WorkspaceId,
                    name = space.Name,
                    slug = space.Slug,
                    color = space.Color,
                    icon = space.Icon,
                    isPrivate = space.IsPrivate,
                    orderKey = space.OrderKey,
                    defaultDocumentId = space.DefaultDocumentId
                }, jsonOptions),
                ClientTraceId = request.TraceId,
                AuthorUserId = creatorId
            });

            // No starter statuses seeded here — Status is workspace-visible everywhere, so a new
            // space just uses whatever statuses the workspace already has (see Status.cs).
            db.SyncEvents.AddRange(events);
            idempotencyService.MarkAsProcessed(request.TraceId);

            logger.LogInformation("Successfully created space {SpaceId} in database with {EventCount} SyncEvents", space.Id, events.Count);
            return Result<long>.Success(0);
        }, cancellationToken);

        if (result.IsSuccess && events.Count > 0)
        {
            var payloads = events.Select(SyncQueryService.MapToPayload).ToArray();

            _ = realtimeService
                .NotifySyncEventBatchAsync(workspaceContext.WorkspaceId, payloads, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time DeltaBatch for space {SpaceId}", space!.Id),
                    TaskContinuationOptions.OnlyOnFaulted);

            return Result<long>.Success(events[^1].Id);
        }

        return Result<long>.Failure(result.Error ?? Error.Failure("Transaction.Failed", "Unknown transaction failure"));
    }
}
