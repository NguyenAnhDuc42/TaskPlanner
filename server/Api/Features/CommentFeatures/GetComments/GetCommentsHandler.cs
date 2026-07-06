using Microsoft.EntityFrameworkCore;
using Dapper;
using System.Text.Json;

namespace Api;

public class GetCommentsHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext, CursorHelper cursorHelper) : IQueryHandler<GetCommentsQuery, PagedResult<CommentRecord>>
{
    public async Task<Result<PagedResult<CommentRecord>>> Handle(GetCommentsQuery request, CancellationToken cancellationToken)
    {
        DecodeCursor(request.Pagination.Cursor, out var cursorTs, out var cursorId);

        const string sql = @"
            SELECT
                c.id AS Id, c.content AS Content, c.creator_id AS CreatorId,
                c.project_task_id AS TaskId, c.parent_comment_id AS ParentCommentId,
                c.is_edited AS IsEdited, c.created_at AS CreatedAt, c.updated_at AS UpdatedAt
            FROM comments c
            INNER JOIN project_tasks t ON t.id = c.project_task_id
            WHERE c.project_task_id = @TaskId
              AND t.project_workspace_id = @WorkspaceId
              AND c.deleted_at IS NULL
              AND (@CursorTimestamp IS NULL OR (c.created_at > @CursorTimestamp OR (c.created_at = @CursorTimestamp AND c.id > @CursorId)))
            ORDER BY c.created_at ASC, c.id ASC
            LIMIT @PageSize;";

        var connection = db.Database.GetDbConnection();
        var parameters = new
        {
            TaskId = request.TaskId,
            WorkspaceId = workspaceContext.WorkspaceId,
            CursorTimestamp = cursorTs,
            CursorId = cursorId,
            PageSize = request.Pagination.PageSize + 1,
        };

        var comments = (await connection.QueryAsync<CommentRecord>(sql, parameters)).AsList();

        var hasMore = comments.Count > request.Pagination.PageSize;
        if (hasMore) comments.RemoveAt(comments.Count - 1);

        string? nextCursor = null;
        if (hasMore && comments.Count > 0)
        {
            var last = comments[^1];
            nextCursor = cursorHelper.EncodeCursor(new CursorData(new Dictionary<string, object>
            {
                { "Timestamp", last.CreatedAt },
                { "Id", last.Id.ToString() }
            }));
        }

        return Result<PagedResult<CommentRecord>>.Success(new PagedResult<CommentRecord>(comments, nextCursor, hasMore));
    }

    private void DecodeCursor(string? cursor, out DateTimeOffset? ts, out Guid? id)
    {
        ts = null;
        id = null;
        if (string.IsNullOrEmpty(cursor)) return;

        var data = cursorHelper.DecodeCursor(cursor);
        if (data?.Values == null) return;

        if (data.Values.TryGetValue("Timestamp", out var tsObj))
        {
            var tsStr = tsObj is JsonElement el ? el.GetString() : tsObj?.ToString();
            if (DateTimeOffset.TryParse(tsStr, out var parsed)) ts = parsed;
        }
        if (data.Values.TryGetValue("Id", out var idObj))
        {
            var idStr = idObj is JsonElement el2 ? el2.GetString() : idObj?.ToString();
            if (Guid.TryParse(idStr, out var parsed)) id = parsed;
        }
    }
}
