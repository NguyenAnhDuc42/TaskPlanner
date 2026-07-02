using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class CreateCommentHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    SyncPermissionService syncPermission,
    RealtimeService realtimeService,
    IdempotencyService idempotencyService,
    ILogger<CreateCommentHandler> logger
) : ICommandHandler<CreateCommentCommand, long>
{
    public async Task<Result<long>> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to create comment on Task {TaskId}", request.ProjectTaskId);

        var task = await db.ProjectTasks.AsNoTracking()
            .Where(t => t.Id == request.ProjectTaskId && t.DeletedAt == null)
            .Select(t => new { t.ProjectSpaceId, t.ProjectWorkspaceId, t.CreatorId })
            .FirstOrDefaultAsync(cancellationToken);

        if (task is null)
        {
            logger.LogWarning("Task {TaskId} not found or deleted", request.ProjectTaskId);
            return Result<long>.Failure(TaskError.NotFound);
        }

        syncPermission.RequireMember();

        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        SyncEvent? syncEvent = null;

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            var hasProcessed = await idempotencyService.HasProcessedAsync(request.TraceId, cancellationToken);
            if (hasProcessed)
            {
                logger.LogInformation("Idempotent bypass for trace {TraceId}. Skipping.", request.TraceId);
                return Result<long>.Success(0);
            }

            var comment = Comment.Create(request.Id, request.Content, memberId, request.ProjectTaskId, request.ParentCommentId);
            db.Comments.Add(comment);

            var syncPayload = JsonSerializer.Serialize(new
            {
                id = comment.Id,
                taskId = comment.ProjectTaskId,
                content = comment.Content,
                isEdited = comment.IsEdited,
                parentCommentId = comment.ParentCommentId,
                creatorId = comment.CreatorId,
                createdAt = comment.CreatedAt
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            syncEvent = new SyncEvent
            {
                ProjectWorkspaceId = task.ProjectWorkspaceId,
                EntityType = SyncEntityType.Comment,
                EntityId = comment.Id,
                Action = SyncAction.C,
                Payload = syncPayload,
                ClientTraceId = request.TraceId,
                AuthorUserId = memberId
            };

            db.SyncEvents.Add(syncEvent);

            idempotencyService.MarkAsProcessed(request.TraceId);

            logger.LogInformation("Successfully created comment {CommentId} in database with SyncEvent", comment.Id);
            return Result<long>.Success(0);
        }, cancellationToken);

        if (result.IsSuccess && syncEvent != null)
        {
            var payload = SyncQueryService.MapToPayload(syncEvent);

            _ = realtimeService
                .NotifySyncEventAsync(task.ProjectWorkspaceId, payload, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time Delta for comment {CommentId}", request.Id),
                    TaskContinuationOptions.OnlyOnFaulted);

            return Result<long>.Success(syncEvent.Id);
        }

        return Result<long>.Failure(result.Error ?? Error.Failure("Transaction.Failed", "Unknown transaction failure"));
    }
}
