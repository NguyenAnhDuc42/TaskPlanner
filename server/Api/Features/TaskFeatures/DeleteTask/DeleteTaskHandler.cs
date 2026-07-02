using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class DeleteTaskHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    SyncPermissionService syncPermission,
    RealtimeService realtimeService,
    IdempotencyService idempotencyService,
    ILogger<DeleteTaskHandler> logger
) : ICommandHandler<DeleteTaskCommand, long>
{
    public async Task<Result<long>> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to delete task {TaskId}", request.TaskId);

        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        var task = await db.ProjectTasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId && t.DeletedAt == null, cancellationToken);

        if (task == null)
        {
            logger.LogWarning("Task {TaskId} not found or already deleted", request.TaskId);
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

            task.SoftDelete();

            var syncPayload = JsonSerializer.Serialize(new { id = task.Id },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            syncEvent = new SyncEvent
            {
                ProjectWorkspaceId = task.ProjectWorkspaceId,
                EntityType = SyncEntityType.Task,
                EntityId = task.Id,
                Action = SyncAction.D,
                Payload = syncPayload,
                ClientTraceId = request.TraceId,
                AuthorUserId = memberId
            };

            db.SyncEvents.Add(syncEvent);

            idempotencyService.MarkAsProcessed(request.TraceId);

            logger.LogInformation("Successfully deleted task {TaskId} in database with SyncEvent", task.Id);
            return Result<long>.Success(0);
        }, cancellationToken);

        if (result.IsSuccess && syncEvent != null)
        {
            var payload = SyncQueryService.MapToPayload(syncEvent);

            _ = realtimeService
                .NotifySyncEventAsync(workspaceContext.WorkspaceId, payload, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time Delta for deleted task {TaskId}", task.Id),
                    TaskContinuationOptions.OnlyOnFaulted);

            return Result<long>.Success(syncEvent.Id);
        }

        return Result<long>.Failure(result.Error ?? Error.Failure("Transaction.Failed", "Unknown transaction failure"));
    }
}
