using Microsoft.EntityFrameworkCore;

namespace Application;

public class GetNodeTasksHandler(TaskPlanDbContext db, CursorHelper cursorHelper) : IQueryHandler<GetNodeTasksQuery, PagedResult<TaskRecord>>
{
    public async Task<Result<PagedResult<TaskRecord>>> Handle(GetNodeTasksQuery request, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                t.id,
                t.name,
                t.status_id,
                t.priority,
                t.order_key,
                t.project_folder_id,
                t.project_space_id,
                t.custom_color AS color,
                t.custom_icon AS icon,
                CASE 
                    WHEN t.project_folder_id IS NOT NULL THEN 'ProjectFolder'
                    ELSE 'ProjectSpace'
                END AS parent_type
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

        var rawTasks = await db.Database.SqlQueryRaw<TaskRecord>(
            sql,
            new Npgsql.NpgsqlParameter("WorkspaceId", request.WorkspaceId),
            new Npgsql.NpgsqlParameter("ParentId", request.ParentId),
            new Npgsql.NpgsqlParameter("ParentType", request.ParentType),
            new Npgsql.NpgsqlParameter("CursorOrderKey", cursorOrderKey ?? (object)DBNull.Value),
            new Npgsql.NpgsqlParameter("CursorTaskId", cursorTaskId != null ? Guid.Parse(cursorTaskId) : (object)DBNull.Value),
            new Npgsql.NpgsqlParameter("PageSize", request.Pagination.PageSize + 1)
        ).ToListAsync(ct);

        var hasMore = rawTasks.Count > request.Pagination.PageSize;
        if (hasMore) rawTasks.RemoveAt(rawTasks.Count - 1);

        var last = rawTasks.LastOrDefault();
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

        return Result<PagedResult<TaskRecord>>.Success(new PagedResult<TaskRecord>(rawTasks, nextCursor, hasMore));
    }
}


