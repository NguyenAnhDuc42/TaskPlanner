using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class DeleteTaskHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    PermissionService permissionService,
    RealtimeService realtimeService,
    IdempotencyService idempotencyService,
    ILogger<DeleteTaskHandler> logger
) : ICommandHandler<DeleteTaskCommand, long>
{
    public async Task<Result<long>> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to delete task {TaskId}", request.TaskId);

        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        var taskData = await db.ProjectTasks
            .Where(t => t.Id == request.TaskId && t.DeletedAt == null)
            .Select(t => new {
                Task = t,
                SpaceIsPrivate = db.ProjectSpaces.Where(s => s.Id == t.ProjectSpaceId).Select(s => s.IsPrivate).FirstOrDefault(),
                CallerAccess = db.EntityAccesses
                    .Where(ea => ea.ProjectSpaceId == t.ProjectSpaceId && ea.WorkspaceMemberId == memberId && ea.DeletedAt == null)
                    .Select(ea => (AccessLevel?)ea.AccessLevel).FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        var task = taskData?.Task;
        if (task == null)
        {
            logger.LogWarning("Task {TaskId} not found or already deleted", request.TaskId);
            return Result<long>.Failure(TaskError.NotFound);
        }

        // Creators (Member+) can delete their own tasks; otherwise require Admin with space access
        bool isCreator = task.CreatorId == memberId;
        var hasAccess = isCreator
            ? workspaceContext.CurrentMember!.Role.IsAtLeast(Role.Member)
            : permissionService.Verify(Role.Admin, taskData!.SpaceIsPrivate, taskData.CallerAccess, AccessLevel.Editor);

        if (!hasAccess)
        {
            logger.LogWarning("Access denied for user {UserId} to delete task {TaskId}", memberId, task.Id);
            return Result<long>.Failure(MemberError.DontHavePermission);
        }

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
