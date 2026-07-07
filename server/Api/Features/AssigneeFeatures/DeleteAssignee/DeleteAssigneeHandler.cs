using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class DeleteAssigneeHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    SyncPermissionService syncPermission,
    RealtimeService realtimeService,
    IdempotencyService idempotencyService,
    ILogger<DeleteAssigneeHandler> logger
) : ICommandHandler<DeleteAssigneeCommand, long>
{
    public async Task<Result<long>> Handle(DeleteAssigneeCommand request, CancellationToken cancellationToken)
    {
        var assigneeData = await db.TaskAssignments
            .Where(a => a.Id == request.AssigneeId && a.DeletedAt == null)
            .Select(a => new { Assignment = a, WorkspaceId = db.ProjectTasks.Where(t => t.Id == a.ProjectTaskId).Select(t => t.ProjectWorkspaceId).FirstOrDefault() })
            .FirstOrDefaultAsync(cancellationToken);

        var assignment = assigneeData?.Assignment;
        if (assignment is null)
            return Result<long>.Failure(AssigneeError.NotFound);

        syncPermission.RequireMember();

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

            assignment.SoftDelete();

            var syncPayload = JsonSerializer.Serialize(new { id = assignment.Id },
                SyncJson.Options);

            syncEvent = new SyncEvent
            {
                ProjectWorkspaceId = assigneeData!.WorkspaceId,
                EntityType = SyncEntityType.Assignee,
                EntityId = assignment.Id,
                Action = SyncAction.D,
                Payload = syncPayload,
                ClientTraceId = request.TraceId,
                AuthorUserId = authorId
            };

            db.SyncEvents.Add(syncEvent);

            idempotencyService.MarkAsProcessed(request.TraceId);

            logger.LogInformation("Successfully deleted assignee {AssigneeId} with SyncEvent", assignment.Id);
            return Result<long>.Success(0);
        }, cancellationToken);

        if (result.IsSuccess && syncEvent != null)
        {
            var payload = SyncQueryService.MapToPayload(syncEvent);

            _ = realtimeService
                .NotifySyncEventAsync(assigneeData!.WorkspaceId, payload, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time Delta for deleted assignee {AssigneeId}", assignment.Id),
                    TaskContinuationOptions.OnlyOnFaulted);

            return Result<long>.Success(syncEvent.Id);
        }

        return Result<long>.Failure(result.Error ?? Error.Failure("Transaction.Failed", "Unknown transaction failure"));
    }
}
