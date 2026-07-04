using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class UpdateSpaceHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    SyncPermissionService syncPermission,
    RealtimeService realtimeService,
    IdempotencyService idempotencyService,
    ILogger<UpdateSpaceHandler> logger
) : ICommandHandler<UpdateSpaceCommand, long>
{
    public async Task<Result<long>> Handle(UpdateSpaceCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to update space {SpaceId}", request.SpaceId);

        var space = await db.ProjectSpaces
            .FirstOrDefaultAsync(s => s.Id == request.SpaceId && s.DeletedAt == null, cancellationToken);

        if (space == null) return Result<long>.Failure(SpaceError.NotFound);
        if (space.ProjectWorkspaceId != workspaceContext.WorkspaceId) return Result<long>.Failure(SpaceError.NotFound);

        syncPermission.RequireCreatorOrAdmin(space.CreatorId ?? Guid.Empty);

        SyncEvent? syncEvent = null;

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            var hasProcessed = await idempotencyService.HasProcessedAsync(request.TraceId, cancellationToken);
            if (hasProcessed) return Result<long>.Success(0);

            var slug = request.Name != null ? SlugHelper.GenerateSlug(request.Name) : null;
            space.Update(request.Name, slug, request.Color, request.Icon, request.IsPrivate, request.OrderKey);

            syncEvent = new SyncEvent
            {
                ProjectWorkspaceId = space.ProjectWorkspaceId,
                EntityType = SyncEntityType.Space,
                EntityId = space.Id,
                Action = SyncAction.U,
                Payload = JsonSerializer.Serialize(new
                {
                    id = space.Id,
                    workspaceId = space.ProjectWorkspaceId,
                    name = space.Name,
                    slug = space.Slug,
                    color = space.Color,
                    icon = space.Icon,
                    isPrivate = space.IsPrivate,
                    orderKey = space.OrderKey,
                    defaultDocumentId = space.DefaultDocumentId,
                    isArchived = space.IsArchived
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                ClientTraceId = request.TraceId,
                AuthorUserId = workspaceContext.CurrentMember?.Id ?? Guid.Empty
            };

            db.SyncEvents.Add(syncEvent);
            idempotencyService.MarkAsProcessed(request.TraceId);
            return Result<long>.Success(0);
        }, cancellationToken);

        if (result.IsSuccess && syncEvent != null)
        {
            var payload = SyncQueryService.MapToPayload(syncEvent);
            _ = realtimeService
                .NotifySyncEventAsync(workspaceContext.WorkspaceId, payload, default)
                .ContinueWith(t => logger.LogError(t.Exception, "Failed to send real-time Delta for space {SpaceId}", space.Id), TaskContinuationOptions.OnlyOnFaulted);
            return Result<long>.Success(syncEvent.Id);
        }

        return Result<long>.Failure(result.Error ?? Error.Failure("Transaction.Failed", "Unknown transaction failure"));
    }
}
