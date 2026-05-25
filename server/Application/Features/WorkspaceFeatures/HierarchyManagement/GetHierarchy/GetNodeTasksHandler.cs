using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Dapper;

namespace Application;

public class GetNodeTasksHandler(TaskPlanDbContext db, CursorHelper cursorHelper) : IQueryHandler<GetNodeTasksQuery, PagedResult<TaskRecord>>
{
    public async Task<Result<PagedResult<TaskRecord>>> Handle(GetNodeTasksQuery request, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                t.id AS Id,
                t.name AS Name,
                t.status_id AS StatusId,
                t.priority AS Priority,
                t.order_key AS OrderKey,
                t.project_folder_id AS ProjectFolderId,
                t.project_space_id AS ProjectSpaceId,
                t.custom_color AS Color,
                t.custom_icon AS Icon,
                CASE 
                    WHEN t.project_folder_id IS NOT NULL THEN 'ProjectFolder'
                    ELSE 'ProjectSpace'
                END AS ParentType
            FROM project_tasks t
            WHERE t.project_workspace_id = @WorkspaceId
              AND t.deleted_at IS NULL
              AND t.is_archived = false
              AND (
                  (@ParentType = 'ProjectFolder' AND t.project_folder_id = @ParentId)
                  OR
                  (@ParentType = 'ProjectSpace' AND t.project_space_id = @ParentId AND t.project_folder_id IS NULL)
              )
              AND (
                  @CursorOrderKey::text IS NULL
                  OR t.order_key > @CursorOrderKey::text
                  OR (t.order_key = @CursorOrderKey::text AND t.id > @CursorTaskId::uuid)
              )
            ORDER BY t.order_key, t.id
            LIMIT @PageSize;";
        var cursorData = request.Pagination.Cursor != null ? cursorHelper.DecodeCursor(request.Pagination.Cursor) : null;
        var cursorOrderKey = cursorData?.Values.GetValueOrDefault("OrderKey")?.ToString();
        var cursorTaskId = cursorData?.Values.GetValueOrDefault("Id")?.ToString();

        var connection = db.Database.GetDbConnection();
        var parameters = new
        {
            WorkspaceId = request.WorkspaceId,
            ParentId = request.ParentId,
            ParentType = request.ParentType,
            CursorOrderKey = cursorOrderKey,
            CursorTaskId = cursorTaskId != null ? (Guid?)Guid.Parse(cursorTaskId) : null,
            PageSize = request.Pagination.PageSize + 1
        };

        var mappedTasks = (await connection.QueryAsync<TaskRecord>(sql, parameters)).AsList();

        var hasMore = mappedTasks.Count > request.Pagination.PageSize;
        if (hasMore) mappedTasks.RemoveAt(mappedTasks.Count - 1);

        var last = mappedTasks.LastOrDefault();
        string? nextCursor = null;

        if (hasMore && last != null)
        {
            var data = new CursorData(new Dictionary<string, object>
            {
                { "OrderKey", last.OrderKey ?? string.Empty },
                { "Id", last.Id.ToString() }
            });
            nextCursor = cursorHelper.EncodeCursor(data);
        }

        return Result<PagedResult<TaskRecord>>.Success(new PagedResult<TaskRecord>(mappedTasks, nextCursor, hasMore));
    }
}


