using Microsoft.EntityFrameworkCore;


namespace Application;

public class AddCommentHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext) : ICommandHandler<AddCommentCommand, CommentRecord>
{
    public async Task<Result<CommentRecord>> Handle(AddCommentCommand request, CancellationToken ct)
    {
        const string sql = "SELECT id FROM project_tasks WHERE id = @TaskId AND project_workspace_id = @WorkspaceId AND deleted_at IS NULL;";
        var parameters = new object[] {
            new Npgsql.NpgsqlParameter("TaskId", request.TaskId),
            new Npgsql.NpgsqlParameter("WorkspaceId", workspaceContext.workspaceId)
        };
        var taskId = await db.Database.SqlQueryRaw<Guid?>(sql, parameters).FirstOrDefaultAsync(ct);
        
        if (taskId == null)
            return Result<CommentRecord>.Failure(Error.NotFound("Task.NotFound", "Task not found."));

        var comment = Comment.Create(request.Content, workspaceContext.CurrentMember.UserId, request.TaskId, request.ParentCommentId);
        db.Comments.Add(comment);
        await db.SaveChangesAsync(ct);

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


