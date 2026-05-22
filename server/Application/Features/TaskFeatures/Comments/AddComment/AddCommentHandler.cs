using Microsoft.EntityFrameworkCore;
using Dapper;

namespace Application;

public class AddCommentHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext) : ICommandHandler<AddCommentCommand, CommentDto>
{
    public async Task<Result<CommentDto>> Handle(AddCommentCommand request, CancellationToken ct)
    {
        const string sql = "SELECT id FROM project_tasks WHERE id = @TaskId AND project_workspace_id = @WorkspaceId AND deleted_at IS NULL;";
        var taskId = await db.Database.GetDbConnection().QuerySingleOrDefaultAsync<Guid?>(sql, new { request.TaskId, WorkspaceId = workspaceContext.workspaceId });
        
        if (taskId == null)
            return Result<CommentDto>.Failure(Error.NotFound("Task.NotFound", "Task not found."));

        var comment = Comment.Create(request.Content, workspaceContext.CurrentMember.UserId, request.TaskId, request.ParentCommentId);
        db.Comments.Add(comment);
        await db.SaveChangesAsync(ct);

        var dto = new CommentDto
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

        return Result<CommentDto>.Success(dto);
    }
}


