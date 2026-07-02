using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class UpdateTaskHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    SyncPermissionService syncPermission,
    RealtimeService realtimeService,
    IdempotencyService idempotencyService,
    ILogger<UpdateTaskHandler> logger
) : ICommandHandler<UpdateTaskCommand, long>
{
    public async Task<Result<long>> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to update task {TaskId}", request.TaskId);

        var task = await db.ProjectTasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId && t.DeletedAt == null, cancellationToken);

        if (task == null)
        {
            logger.LogWarning("Task {TaskId} not found or deleted", request.TaskId);
            return Result<long>.Failure(TaskError.NotFound);
        }

        syncPermission.RequireCreatorOrAdmin(task.CreatorId ?? Guid.Empty);

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

            task.Update(
                name: request.Name,
                slug: slug,
                color: request.Color,
                icon: request.Icon,
                statusId: request.StatusId,
                priority: request.Priority,
                startDate: request.StartDate,
                clearStartDate: request.ClearStartDate,
                dueDate: request.DueDate,
                clearDueDate: request.ClearDueDate,
                storyPoints: request.StoryPoints,
                timeEstimateSeconds: request.TimeEstimateSeconds,
                orderKey: request.OrderKey,
                parentTaskId: request.ParentTaskId
            );

            var syncPayload = JsonSerializer.Serialize(new
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
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            syncEvent = new SyncEvent
            {
                ProjectWorkspaceId = task.ProjectWorkspaceId,
                EntityType = SyncEntityType.Task,
                EntityId = task.Id,
                Action = SyncAction.U,
                Payload = syncPayload,
                ClientTraceId = request.TraceId,
                AuthorUserId = workspaceContext.CurrentMember?.Id ?? Guid.Empty
            };

            db.SyncEvents.Add(syncEvent);

            idempotencyService.MarkAsProcessed(request.TraceId);

            logger.LogInformation("Successfully updated task {TaskId} in database with SyncEvent", task.Id);
            return Result<long>.Success(0);
        }, cancellationToken);

        if (result.IsSuccess && syncEvent != null)
        {
            var payload = SyncQueryService.MapToPayload(syncEvent);

            _ = realtimeService
                .NotifySyncEventAsync(workspaceContext.WorkspaceId, payload, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time Delta for task {TaskId}", task.Id),
                    TaskContinuationOptions.OnlyOnFaulted);

            return Result<long>.Success(syncEvent.Id);
        }

        return Result<long>.Failure(result.Error ?? Error.Failure("Transaction.Failed", "Unknown transaction failure"));
    }
}
