using Microsoft.EntityFrameworkCore;


namespace Application;

public class AddCommentHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext) : ICommandHandler<AddCommentCommand, CommentRecord>
{
    public async Task<Result<CommentRecord>> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        var taskExists = await db.ProjectTasks
            .AnyAsync(t => t.Id == request.TaskId && t.ProjectWorkspaceId == workspaceContext.workspaceId && t.DeletedAt == null, cancellationToken);
        
        if (!taskExists)
            return Result<CommentRecord>.Failure(Error.NotFound("Task.NotFound", "Task not found."));

        var comment = Comment.Create(request.Content, workspaceContext.CurrentMember.UserId, request.TaskId, request.ParentCommentId);
        db.Comments.Add(comment);
        await db.SaveChangesAsync(cancellationToken);

        var dto = new CommentRecord
        {
            Id = comment.Id,
            Content = comment.Content,
            CreatorId = comment.CreatorId ?? Guid.Empty,
            ProjectTaskId = comment.ProjectTaskId,
            ParentCommentId = comment.ParentCommentId,
            IsEdited = comment.IsEdited,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt
        };

        return Result<CommentRecord>.Success(dto);
    }
}


