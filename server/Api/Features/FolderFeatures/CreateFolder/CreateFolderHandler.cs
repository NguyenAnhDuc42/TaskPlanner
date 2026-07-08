using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class CreateFolderHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    SyncPermissionService syncPermission,
    RealtimeService realtimeService,
    IdempotencyService idempotencyService,
    ILogger<CreateFolderHandler> logger
) : ICommandHandler<CreateFolderCommand, long>
{
    public async Task<Result<long>> Handle(CreateFolderCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to create folder '{FolderName}' in space {SpaceId}", request.Name, request.SpaceId);

        var space = await db.ProjectSpaces
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SpaceId && s.DeletedAt == null, cancellationToken);
        if (space is null)
        {
            logger.LogWarning("Space {SpaceId} not found or deleted", request.SpaceId);
            return Result<long>.Failure(SpaceError.NotFound);
        }

        syncPermission.RequireMember();

        var creatorId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        ProjectFolder? folder = null;
        SyncEvent? syncEvent = null;

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            var hasProcessed = await idempotencyService.HasProcessedAsync(request.TraceId, cancellationToken);
            if (hasProcessed)
            {
                logger.LogInformation("Idempotent bypass for trace {TraceId}. Skipping.", request.TraceId);
                return Result<long>.Success(0);
            }

            var maxKey = await db.ProjectFolders
                .AsNoTracking()
                .Where(f => f.ProjectSpaceId == request.SpaceId && f.DeletedAt == null)
                .Select(f => (string?)f.OrderKey)
                .OrderByDescending(k => k)
                .FirstOrDefaultAsync(cancellationToken);

            var orderKey = FractionalIndex.SafeAfter(maxKey);
            var slug = SlugHelper.GenerateSlug(request.Name);

            folder = ProjectFolder.Create(
                id: request.Id,
                projectWorkspaceId: workspaceContext.WorkspaceId,
                projectSpaceId: request.SpaceId,
                name: request.Name,
                slug: slug,
                orderKey: orderKey,
                creatorId: creatorId,
                color: request.Color,
                icon: request.Icon,
                startDate: request.StartDate,
                dueDate: request.DueDate
            );
            db.ProjectFolders.Add(folder);

            var syncPayload = JsonSerializer.Serialize(new
            {
                id = folder.Id,
                workspaceId = workspaceContext.WorkspaceId,
                spaceId = folder.ProjectSpaceId,
                name = folder.Name,
                slug = folder.Slug,
                color = folder.Color,
                icon = folder.Icon,
                orderKey = folder.OrderKey,
                startDate = folder.StartDate,
                dueDate = folder.DueDate,
                createdAt = folder.CreatedAt
            }, SyncJson.Options);

            syncEvent = new SyncEvent
            {
                ProjectWorkspaceId = workspaceContext.WorkspaceId,
                EntityType = SyncEntityType.Folder,
                EntityId = folder.Id,
                Action = SyncAction.C,
                Payload = syncPayload,
                ClientTraceId = request.TraceId,
                AuthorUserId = creatorId
            };

            db.SyncEvents.Add(syncEvent);
            idempotencyService.MarkAsProcessed(request.TraceId);

            logger.LogInformation("Successfully created folder {FolderId} in database with SyncEvent", folder.Id);
            return Result<long>.Success(0);
        }, cancellationToken);

        if (result.IsSuccess && syncEvent != null)
        {
            var payload = SyncQueryService.MapToPayload(syncEvent);

            _ = realtimeService
                .NotifySyncEventAsync(workspaceContext.WorkspaceId, payload, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time Delta for folder {FolderId}", folder!.Id),
                    TaskContinuationOptions.OnlyOnFaulted);

            return Result<long>.Success(syncEvent.Id);
        }

        return Result<long>.Failure(result.Error ?? Error.Failure("Transaction.Failed", "Unknown transaction failure"));
    }
}
