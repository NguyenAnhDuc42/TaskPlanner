using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class UpdateFolderHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    SyncPermissionService syncPermission,
    RealtimeService realtimeService,
    IdempotencyService idempotencyService,
    ILogger<UpdateFolderHandler> logger
) : ICommandHandler<UpdateFolderCommand, long>
{
    public async Task<Result<long>> Handle(UpdateFolderCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to update folder {FolderId}", request.FolderId);

        var folder = await db.ProjectFolders
            .FirstOrDefaultAsync(f => f.Id == request.FolderId && f.DeletedAt == null, cancellationToken);

        if (folder == null)
        {
            logger.LogWarning("Folder {FolderId} not found or deleted", request.FolderId);
            return Result<long>.Failure(FolderError.NotFound);
        }

        syncPermission.RequireCreatorOrAdmin(folder.CreatorId ?? Guid.Empty);

        SyncEvent? syncEvent = null;

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            var hasProcessed = await idempotencyService.HasProcessedAsync(request.TraceId, cancellationToken);
            if (hasProcessed)
            {
                logger.LogInformation("Idempotent bypass for trace {TraceId}. Skipping.", request.TraceId);
                return Result<long>.Success(0);
            }

            var slug = request.Name != null ? SlugHelper.GenerateSlug(request.Name) : null;

            folder.Update(
                name: request.Name,
                slug: slug,
                color: request.Color,
                icon: request.Icon,
                startDate: request.StartDate,
                dueDate: request.DueDate,
                orderKey: request.OrderKey,
                clearStartDate: request.ClearStartDate,
                clearDueDate: request.ClearDueDate
            );

            var syncPayload = JsonSerializer.Serialize(new
            {
                id = folder.Id,
                workspaceId = folder.ProjectWorkspaceId,
                spaceId = folder.ProjectSpaceId,
                name = folder.Name,
                slug = folder.Slug,
                color = folder.Color,
                icon = folder.Icon,
                orderKey = folder.OrderKey,
                startDate = folder.StartDate,
                dueDate = folder.DueDate
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            syncEvent = new SyncEvent
            {
                ProjectWorkspaceId = folder.ProjectWorkspaceId,
                EntityType = SyncEntityType.Folder,
                EntityId = folder.Id,
                Action = SyncAction.U,
                Payload = syncPayload,
                ClientTraceId = request.TraceId,
                AuthorUserId = workspaceContext.CurrentMember?.Id ?? Guid.Empty
            };

            db.SyncEvents.Add(syncEvent);
            idempotencyService.MarkAsProcessed(request.TraceId);

            logger.LogInformation("Successfully updated folder {FolderId} in database with SyncEvent", folder.Id);
            return Result<long>.Success(0);
        }, cancellationToken);

        if (result.IsSuccess && syncEvent != null)
        {
            var payload = SyncQueryService.MapToPayload(syncEvent);

            _ = realtimeService
                .NotifySyncEventAsync(workspaceContext.WorkspaceId, payload, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time Delta for folder {FolderId}", folder.Id),
                    TaskContinuationOptions.OnlyOnFaulted);

            return Result<long>.Success(syncEvent.Id);
        }

        return Result<long>.Failure(result.Error ?? Error.Failure("Transaction.Failed", "Unknown transaction failure"));
    }
}
