using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application;

public class AddCommentHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    PermissionService permissionService,
    RealtimeService realtimeService,
    ILogger<AddCommentHandler> logger

) : ICommandHandler<AddCommentCommand, CommentRecord>
{
    public async Task<Result<CommentRecord>> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        var task = await db.ProjectTasks
            .AsNoTracking()
            .Where(t => t.Id == request.TaskId && t.DeletedAt == null)
            .Select(t => new { t.ProjectSpaceId, t.CreatorId })
            .FirstOrDefaultAsync(cancellationToken);
        
        if (task == null)
            return Result<CommentRecord>.Failure(Error.NotFound("Task.NotFound", "Task not found."));

        var hasAccess = await permissionService.VerifyAsync(Role.Member, task.ProjectSpaceId, AccessLevel.Viewer, task.CreatorId, cancellationToken);
        if (!hasAccess)
            return Result<CommentRecord>.Failure(MemberError.DontHavePermission);

        var comment = Comment.Create(request.Content, workspaceContext.CurrentMember.UserId, request.TaskId, request.ParentCommentId);
        db.Comments.Add(comment);
        
        var affected = await db.SaveChangesAsync(cancellationToken);

        var dto = new CommentRecord
        {
            Id = comment.Id,
            Content = comment.Content,
            CreatorId = comment.CreatorId ?? Guid.Empty,
            TaskId = comment.ProjectTaskId,
            ParentCommentId = comment.ParentCommentId,
            IsEdited = comment.IsEdited,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt
        };

        if (affected > 0)
        {
            _ = realtimeService
            .NotifyEntitiesUpdatedAsync(
                workspaceContext.WorkspaceId,
                new EntityBatchUpdate { Comments = [dto] },
                default)
            .ContinueWith(t =>
                logger.LogError(t.Exception, "Failed to send real-time notification for added comment {CommentId}", comment.Id),
                TaskContinuationOptions.OnlyOnFaulted);
        }

        return Result<CommentRecord>.Success(dto);
    }
}


