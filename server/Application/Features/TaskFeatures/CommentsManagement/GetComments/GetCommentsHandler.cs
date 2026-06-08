using Microsoft.EntityFrameworkCore;
using Dapper;


namespace Application;

public class GetCommentsHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext) : IQueryHandler<GetCommentsQuery, List<CommentRecord>>
{
    public async Task<Result<List<CommentRecord>>> Handle(GetCommentsQuery request, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT 
                c.id AS Id, c.content AS Content, c.created_by_id AS CreatorId,
                c.project_task_id AS TaskId, c.parent_comment_id AS ParentCommentId,
                c.is_edited AS IsEdited, c.created_at AS CreatedAt, c.updated_at AS UpdatedAt
            FROM comments c
            INNER JOIN project_tasks t ON t.id = c.project_task_id
            WHERE c.project_task_id = @TaskId 
              AND t.project_workspace_id = @WorkspaceId 
              AND c.deleted_at IS NULL
            ORDER BY c.created_at ASC;";

        var parameters = new {
            TaskId = request.TaskId,
            WorkspaceId = workspaceContext.WorkspaceId
        };

        var connection = db.Database.GetDbConnection();
        var comments = (await connection.QueryAsync<CommentRecord>(sql, parameters)).AsList();

        return Result<List<CommentRecord>>.Success(comments);
    }
}


