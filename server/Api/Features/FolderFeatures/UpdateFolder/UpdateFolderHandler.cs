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

        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        var oldSpaceId = folder.ProjectSpaceId;
        List<SyncEvent> events = [];

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
                clearDueDate: request.ClearDueDate,
                spaceId: request.SpaceId
            );

            var jsonOptions = SyncJson.Options;

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
            }, jsonOptions);

            events.Add(new SyncEvent
            {
                ProjectWorkspaceId = folder.ProjectWorkspaceId,
                EntityType = SyncEntityType.Folder,
                EntityId = folder.Id,
                Action = SyncAction.U,
                Payload = syncPayload,
                ClientTraceId = request.TraceId,
                AuthorUserId = memberId
            });

            // Cascade: a folder moving to a different space takes its tasks with it. Without
            // this, tasks silently kept referencing the old space — invisible on the new space's
            // board, and wrongly swept up when the OLD space (or its statuses) got deleted later.
            if (oldSpaceId != folder.ProjectSpaceId)
            {
                var childTasks = await db.ProjectTasks
                    .Where(t => t.ProjectFolderId == folder.Id && t.DeletedAt == null)
                    .ToListAsync(cancellationToken);

                foreach (var task in childTasks)
                {
                    task.Update(spaceId: folder.ProjectSpaceId);

                    events.Add(new SyncEvent
                    {
                        ProjectWorkspaceId = task.ProjectWorkspaceId,
                        EntityType = SyncEntityType.Task,
                        EntityId = task.Id,
                        Action = SyncAction.U,
                        Payload = JsonSerializer.Serialize(new
                        {
                            id = task.Id,
                            workspaceId = task.ProjectWorkspaceId,
                            spaceId = task.ProjectSpaceId,
                            folderId = task.ProjectFolderId,
                            name = task.Name,
                            slug = task.Slug,
                            defaultDocumentId = task.DefaultDocumentId,
                            color = task.Color,
                            icon = task.Icon,
                            statusId = task.StatusId,
                            priority = task.Priority,
                            startDate = task.StartDate,
                            dueDate = task.DueDate,
                            storyPoints = task.StoryPoints,
                            timeEstimateSeconds = task.TimeEstimateSeconds,
                            orderKey = task.OrderKey,
                            parentTaskId = task.ParentTaskId,
                            isArchived = task.IsArchived
                        }, jsonOptions),
                        ClientTraceId = request.TraceId,
                        AuthorUserId = memberId
                    });
                }
            }

            db.SyncEvents.AddRange(events);
            idempotencyService.MarkAsProcessed(request.TraceId);

            logger.LogInformation("Successfully updated folder {FolderId} in database with {EventCount} SyncEvents", folder.Id, events.Count);
            return Result<long>.Success(0);
        }, cancellationToken);

        if (result.IsSuccess && events.Count > 0)
        {
            var payloads = events.Select(SyncQueryService.MapToPayload).ToArray();

            _ = realtimeService
                .NotifySyncEventBatchAsync(workspaceContext.WorkspaceId, payloads, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time DeltaBatch for folder {FolderId}", folder.Id),
                    TaskContinuationOptions.OnlyOnFaulted);

            return Result<long>.Success(events[^1].Id);
        }

        return Result<long>.Failure(result.Error ?? Error.Failure("Transaction.Failed", "Unknown transaction failure"));
    }
}
