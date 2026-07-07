using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class DeleteCommentHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    SyncPermissionService syncPermission,
    RealtimeService realtimeService,
    IdempotencyService idempotencyService,
    ILogger<DeleteCommentHandler> logger
) : ICommandHandler<DeleteCommentCommand, long>
{
    public async Task<Result<long>> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to delete comment {CommentId}", request.CommentId);

        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;

        var commentData = await (
            from c in db.Comments
            where c.Id == request.CommentId && c.DeletedAt == null
            join t in db.ProjectTasks on c.ProjectTaskId equals t.Id
            select new
            {
                Comment = c,
                WorkspaceId = t.ProjectWorkspaceId
            }
        ).FirstOrDefaultAsync(cancellationToken);

        var comment = commentData?.Comment;
        if (comment == null)
        {
            logger.LogWarning("Comment {CommentId} not found or already deleted", request.CommentId);
            return Result<long>.Failure(CommentError.NotFound);
        }

        // Author can always delete their own comment; otherwise requires Admin.
        syncPermission.RequireCreatorOrAdmin(comment.CreatorId ?? Guid.Empty);

        SyncEvent? syncEvent = null;

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            var hasProcessed = await idempotencyService.HasProcessedAsync(request.TraceId, cancellationToken);
            if (hasProcessed)
            {
                logger.LogInformation("Idempotent bypass for trace {TraceId}. Skipping.", request.TraceId);
                return Result<long>.Success(0);
            }

            comment.SoftDelete();

            var syncPayload = JsonSerializer.Serialize(new { id = comment.Id },
                SyncJson.Options);

            syncEvent = new SyncEvent
            {
                ProjectWorkspaceId = commentData!.WorkspaceId,
                EntityType = SyncEntityType.Comment,
                EntityId = comment.Id,
                Action = SyncAction.D,
                Payload = syncPayload,
                ClientTraceId = request.TraceId,
                AuthorUserId = memberId
            };

            db.SyncEvents.Add(syncEvent);

            idempotencyService.MarkAsProcessed(request.TraceId);

            logger.LogInformation("Successfully deleted comment {CommentId} in database with SyncEvent", comment.Id);
            return Result<long>.Success(0);
        }, cancellationToken);

        if (result.IsSuccess && syncEvent != null)
        {
            var payload = SyncQueryService.MapToPayload(syncEvent);

            _ = realtimeService
                .NotifySyncEventAsync(commentData!.WorkspaceId, payload, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time Delta for deleted comment {CommentId}", comment.Id),
                    TaskContinuationOptions.OnlyOnFaulted);

            return Result<long>.Success(syncEvent.Id);
        }

        return Result<long>.Failure(result.Error ?? Error.Failure("Transaction.Failed", "Unknown transaction failure"));
    }
}
