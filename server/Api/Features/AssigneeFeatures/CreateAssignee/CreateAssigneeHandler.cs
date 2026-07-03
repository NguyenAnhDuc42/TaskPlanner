using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class CreateAssigneeHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    SyncPermissionService syncPermission,
    RealtimeService realtimeService,
    NotificationService notificationService,
    IdempotencyService idempotencyService,
    ILogger<CreateAssigneeHandler> logger
) : ICommandHandler<CreateAssigneeCommand, long>
{
    public async Task<Result<long>> Handle(CreateAssigneeCommand request, CancellationToken cancellationToken)
    {
        var task = await db.ProjectTasks.AsNoTracking()
            .Where(t => t.Id == request.TaskId && t.DeletedAt == null)
            .Select(t => new { t.ProjectWorkspaceId, t.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (task is null)
            return Result<long>.Failure(TaskError.NotFound);

        var isValidMember = await db.WorkspaceMembers.AnyAsync(
            m => m.Id == request.MemberId && m.ProjectWorkspaceId == task.ProjectWorkspaceId && m.DeletedAt == null, cancellationToken);
        if (!isValidMember)
            return Result<long>.Failure(MemberError.NotFound);

        syncPermission.RequireMember();

        var alreadyAssigned = await db.TaskAssignments.AnyAsync(
            a => a.ProjectTaskId == request.TaskId && a.WorkspaceMemberId == request.MemberId && a.DeletedAt == null, cancellationToken);
        if (alreadyAssigned)
        {
            logger.LogInformation("Member {MemberId} is already assigned to Task {TaskId}, skipping duplicate", request.MemberId, request.TaskId);
            return Result<long>.Success(0);
        }

        var authorId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        SyncEvent? syncEvent = null;

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            var hasProcessed = await idempotencyService.HasProcessedAsync(request.TraceId, cancellationToken);
            if (hasProcessed)
            {
                logger.LogInformation("Idempotent bypass for trace {TraceId}. Skipping.", request.TraceId);
                return Result<long>.Success(0);
            }

            var assignment = TaskAssignment.Create(request.Id, request.TaskId, request.MemberId, authorId);
            db.TaskAssignments.Add(assignment);

            var syncPayload = JsonSerializer.Serialize(new
            {
                id = assignment.Id,
                taskId = assignment.ProjectTaskId,
                workspaceMemberId = assignment.WorkspaceMemberId
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            syncEvent = new SyncEvent
            {
                ProjectWorkspaceId = task.ProjectWorkspaceId,
                EntityType = SyncEntityType.Assignee,
                EntityId = assignment.Id,
                Action = SyncAction.C,
                Payload = syncPayload,
                ClientTraceId = request.TraceId,
                AuthorUserId = authorId
            };

            db.SyncEvents.Add(syncEvent);

            idempotencyService.MarkAsProcessed(request.TraceId);

            logger.LogInformation("Successfully created assignee {AssigneeId} on Task {TaskId} with SyncEvent", assignment.Id, request.TaskId);
            return Result<long>.Success(0);
        }, cancellationToken);

        if (result.IsSuccess && syncEvent != null)
        {
            var payload = SyncQueryService.MapToPayload(syncEvent);

            _ = realtimeService
                .NotifySyncEventAsync(task.ProjectWorkspaceId, payload, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time Delta for assignee {AssigneeId}", request.Id),
                    TaskContinuationOptions.OnlyOnFaulted);

            // Notify the assignee — mirrors the legacy UpdateTaskAssigneesHandler's behavior,
            // which this Sync-engine handler otherwise fully replaces. Skipped for self-assignment.
            var actorUserId = workspaceContext.CurrentMember?.UserId ?? Guid.Empty;
            var recipientUserId = await db.WorkspaceMembers.AsNoTracking()
                .Where(m => m.Id == request.MemberId)
                .Select(m => m.UserId)
                .FirstOrDefaultAsync(cancellationToken);

            if (recipientUserId != Guid.Empty && recipientUserId != actorUserId)
            {
                var actorName = await db.Users.AsNoTracking()
                    .Where(u => u.Id == actorUserId)
                    .Select(u => u.Name)
                    .FirstOrDefaultAsync(cancellationToken);

                _ = notificationService.PushAsync(
                    recipientUserId, actorUserId, task.ProjectWorkspaceId,
                    "task_assigned", "task", request.TaskId,
                    $"{actorName ?? "Someone"} assigned you to \"{task.Name}\"",
                    cancellationToken: cancellationToken);
            }

            return Result<long>.Success(syncEvent.Id);
        }

        return Result<long>.Failure(result.Error ?? Error.Failure("Transaction.Failed", "Unknown transaction failure"));
    }
}
