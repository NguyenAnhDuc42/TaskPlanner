using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class UpdateCommentHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    RealtimeService realtimeService,
    IdempotencyService idempotencyService,
    ILogger<UpdateCommentHandler> logger
) : ICommandHandler<UpdateCommentCommand, long>
{
    public async Task<Result<long>> Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to update comment {CommentId}", request.CommentId);

        var commentData = await db.Comments
            .Where(c => c.Id == request.CommentId && c.DeletedAt == null)
            .Select(c => new { Comment = c, WorkspaceId = db.ProjectTasks.Where(t => t.Id == c.ProjectTaskId).Select(t => t.ProjectWorkspaceId).FirstOrDefault() })
            .FirstOrDefaultAsync(cancellationToken);

        var comment = commentData?.Comment;
        if (comment == null)
        {
            logger.LogWarning("Comment {CommentId} not found or deleted", request.CommentId);
            return Result<long>.Failure(CommentError.NotFound);
        }

        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;

        // Only the author may edit their own comment content.
        if (comment.CreatorId != memberId)
        {
            logger.LogWarning("Access denied for user {UserId} to update comment {CommentId}", memberId, comment.Id);
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

            comment.UpdateContent(request.Content);

            var syncPayload = JsonSerializer.Serialize(new
            {
                id = comment.Id,
                taskId = comment.ProjectTaskId,
                content = comment.Content,
                isEdited = comment.IsEdited,
                parentCommentId = comment.ParentCommentId,
                creatorId = comment.CreatorId,
                createdAt = comment.CreatedAt
            }, SyncJson.Options);

            syncEvent = new SyncEvent
            {
                ProjectWorkspaceId = commentData!.WorkspaceId,
                EntityType = SyncEntityType.Comment,
                EntityId = comment.Id,
                Action = SyncAction.U,
                Payload = syncPayload,
                ClientTraceId = request.TraceId,
                AuthorUserId = memberId
            };

            db.SyncEvents.Add(syncEvent);

            idempotencyService.MarkAsProcessed(request.TraceId);

            logger.LogInformation("Successfully updated comment {CommentId} in database with SyncEvent", comment.Id);
            return Result<long>.Success(0);
        }, cancellationToken);

        if (result.IsSuccess && syncEvent != null)
        {
            var payload = SyncQueryService.MapToPayload(syncEvent);

            _ = realtimeService
                .NotifySyncEventAsync(commentData!.WorkspaceId, payload, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time Delta for comment {CommentId}", comment.Id),
                    TaskContinuationOptions.OnlyOnFaulted);

            return Result<long>.Success(syncEvent.Id);
        }

        return Result<long>.Failure(result.Error ?? Error.Failure("Transaction.Failed", "Unknown transaction failure"));
    }
}
