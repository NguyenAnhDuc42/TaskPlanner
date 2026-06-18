using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application;

public class DeleteCommentHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    PermissionService permissionService,
    RealtimeService realtimeService,
    ILogger<DeleteCommentHandler> logger
) : ICommandHandler<DeleteCommentCommand>
{
    public async Task<Result> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await db.Comments.FirstOrDefaultAsync(c => c.Id == request.CommentId && c.ProjectTaskId == request.TaskId, cancellationToken);
        if (comment == null) return Result.Failure(TaskError.NotFound);

        // Optional: Ensure the user is the creator or has editor rights
        if (comment.CreatorId != workspaceContext.CurrentMember.UserId)
        {
            var task = await db.ProjectTasks.FindAsync(new object[] { request.TaskId }, cancellationToken);
            if (task == null) return Result.Failure(TaskError.NotFound);
            var hasAccess = await permissionService.VerifyAsync(Role.Member, task.ProjectSpaceId, AccessLevel.Editor, task.CreatorId, cancellationToken);
            if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);
        }

        db.Comments.Remove(comment);
        var affected = await db.SaveChangesAsync(cancellationToken);
        if (affected > 0)
        {
            await realtimeService
            .NotifyEntitiesDeletedAsync(
                workspaceContext.WorkspaceId,
                new EntityBatchDelete { CommentIds = [comment.Id] },
                cancellationToken)
            .ContinueWith(t =>
                logger.LogError(t.Exception, "Failed to send real-time notification for deleted comment {CommentId}", comment.Id),
                TaskContinuationOptions.OnlyOnFaulted);
        }

        return Result.Success();
    }
}
