using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class DeleteFolderHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    SyncPermissionService syncPermission,
    RealtimeService realtimeService,
    IdempotencyService idempotencyService,
    ILogger<DeleteFolderHandler> logger
) : ICommandHandler<DeleteFolderCommand, long>
{
    public async Task<Result<long>> Handle(DeleteFolderCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to delete folder {FolderId}", request.FolderId);

        var folder = await db.ProjectFolders
            .FirstOrDefaultAsync(f => f.Id == request.FolderId && f.DeletedAt == null, cancellationToken);

        if (folder == null)
        {
            logger.LogWarning("Folder {FolderId} not found or already deleted", request.FolderId);
            return Result<long>.Failure(FolderError.NotFound);
        }

        syncPermission.RequireCreatorOrAdmin(folder.CreatorId ?? Guid.Empty);

        List<SyncEvent> events = [];
        var creatorId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            var hasProcessed = await idempotencyService.HasProcessedAsync(request.TraceId, cancellationToken);
            if (hasProcessed)
            {
                logger.LogInformation("Idempotent bypass for trace {TraceId}. Skipping.", request.TraceId);
                return Result<long>.Success(0);
            }

            var jsonOptions = SyncJson.Options;

            // Tasks in this folder get moved to space level — otherwise they become invisible in the hierarchy
            var orphanedTasks = await db.ProjectTasks
                .AsNoTracking()
                .Where(t => t.ProjectFolderId == request.FolderId && t.DeletedAt == null)
                .ToListAsync(cancellationToken);

            if (orphanedTasks.Count > 0)
            {
                await db.ProjectTasks
                    .Where(t => t.ProjectFolderId == request.FolderId && t.DeletedAt == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.ProjectFolderId, (Guid?)null), cancellationToken);
            }

            foreach (var task in orphanedTasks)
            {
                events.Add(new SyncEvent
                {
                    ProjectWorkspaceId = workspaceContext.WorkspaceId,
                    EntityType = SyncEntityType.Task,
                    EntityId = task.Id,
                    Action = SyncAction.U,
                    Payload = JsonSerializer.Serialize(new
                    {
                        id = task.Id,
                        workspaceId = task.ProjectWorkspaceId,
                        spaceId = task.ProjectSpaceId,
                        folderId = (Guid?)null,
                        name = task.Name,
                        slug = task.Slug,
                        defaultDocumentId = task.DefaultDocumentId,
                        color = task.Color,
                        icon = task.Icon,
                        statusId = task.StatusId,
                        priority = task.Priority,
                        orderKey = task.OrderKey,
                        parentTaskId = task.ParentTaskId,
                        isArchived = task.IsArchived
                    }, jsonOptions),
                    ClientTraceId = request.TraceId,
                    AuthorUserId = creatorId
                });
            }

            folder.Delete();

            events.Add(new SyncEvent
            {
                ProjectWorkspaceId = workspaceContext.WorkspaceId,
                EntityType = SyncEntityType.Folder,
                EntityId = folder.Id,
                Action = SyncAction.D,
                Payload = JsonSerializer.Serialize(new { id = folder.Id }, jsonOptions),
                ClientTraceId = request.TraceId,
                AuthorUserId = creatorId
            });

            db.SyncEvents.AddRange(events);
            idempotencyService.MarkAsProcessed(request.TraceId);

            logger.LogInformation("Successfully deleted folder {FolderId} in database with {EventCount} SyncEvents", folder.Id, events.Count);
            return Result<long>.Success(0);
        }, cancellationToken);

        if (result.IsSuccess && events.Count > 0)
        {
            var payloads = events.Select(SyncQueryService.MapToPayload).ToArray();

            _ = realtimeService
                .NotifySyncEventBatchAsync(workspaceContext.WorkspaceId, payloads, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time DeltaBatch for deleted folder {FolderId}", folder.Id),
                    TaskContinuationOptions.OnlyOnFaulted);

            return Result<long>.Success(events[^1].Id);
        }

        return Result<long>.Failure(result.Error ?? Error.Failure("Transaction.Failed", "Unknown transaction failure"));
    }
}
