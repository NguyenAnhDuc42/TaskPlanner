using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Interfaces.Data;
using Application.Helpers;
using Dapper;

namespace Application.Features.TaskFeatures;

public class GetCommentsHandler(IDataBase db, WorkspaceContext workspaceContext) : IQueryHandler<GetCommentsQuery, List<CommentDto>>
{
    public async Task<Result<List<CommentDto>>> Handle(GetCommentsQuery request, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                c.id AS Id, c.content AS Content, c.created_by_id AS CreatorId,
                c.project_task_id AS ProjectTaskId, c.parent_comment_id AS ParentCommentId,
                c.is_edited AS IsEdited, c.created_at AS CreatedAt, c.updated_at AS UpdatedAt
            FROM comments c
            INNER JOIN project_tasks t ON t.id = c.project_task_id
            WHERE c.project_task_id = @TaskId 
              AND t.project_workspace_id = @WorkspaceId 
              AND c.deleted_at IS NULL
            ORDER BY c.created_at ASC;";

        var comments = await db.Connection.QueryAsync<CommentDto>(sql, new { 
            request.TaskId, 
            WorkspaceId = workspaceContext.workspaceId 
        });

        return Result<List<CommentDto>>.Success(comments.ToList());
    }
}
