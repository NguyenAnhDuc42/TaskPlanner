using Microsoft.EntityFrameworkCore;

namespace Application;

public class GetNodeSpacesHandler(TaskPlanDbContext db, CursorHelper cursorHelper) : IQueryHandler<GetNodeSpacesQuery, PagedResult<SpaceRecord>>
{
    public async Task<Result<PagedResult<SpaceRecord>>> Handle(GetNodeSpacesQuery request, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                id,
                name,
                custom_color AS color,
                custom_icon  AS icon,
                is_private,
                order_key,
                EXISTS (
                    SELECT 1 FROM project_folders f
                    WHERE f.project_space_id = project_spaces.id
                      AND f.deleted_at IS NULL
                      AND f.is_archived = false
                    LIMIT 1
                ) AS has_folders,
                EXISTS (
                    SELECT 1 FROM project_tasks t
                    WHERE t.project_space_id = project_spaces.id
                      AND t.project_folder_id IS NULL
                      AND t.deleted_at IS NULL
                      AND t.is_archived = false
                    LIMIT 1
                ) AS has_tasks
            FROM project_spaces
            WHERE project_workspace_id = @WorkspaceId
              AND deleted_at IS NULL
              AND is_archived = false
              AND (
                  @CursorOrderKey::text IS NULL
                  OR order_key > @CursorOrderKey::text
                  OR (order_key = @CursorOrderKey::text AND id > @CursorSpaceId::uuid)
              )
            ORDER BY order_key, id
            LIMIT @PageSize;";
        var cursorData = request.Pagination.Cursor != null ? cursorHelper.DecodeCursor(request.Pagination.Cursor) : null;
        var cursorOrderKey = cursorData?.Values.GetValueOrDefault("OrderKey")?.ToString();
        var cursorSpaceId = cursorData?.Values.GetValueOrDefault("Id")?.ToString();

        var rawSpaces = await db.Database.SqlQueryRaw<SpaceRecord>(
            sql,
            new Npgsql.NpgsqlParameter("WorkspaceId", request.WorkspaceId),
            new Npgsql.NpgsqlParameter("CursorOrderKey", cursorOrderKey ?? (object)DBNull.Value),
            new Npgsql.NpgsqlParameter("CursorSpaceId", cursorSpaceId != null ? Guid.Parse(cursorSpaceId) : (object)DBNull.Value),
            new Npgsql.NpgsqlParameter("PageSize", request.Pagination.PageSize + 1)
        ).ToListAsync(ct);

        var hasMore = rawSpaces.Count > request.Pagination.PageSize;
        if (hasMore) rawSpaces.RemoveAt(rawSpaces.Count - 1);

        var last = rawSpaces.LastOrDefault();
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

        return Result<PagedResult<SpaceRecord>>.Success(new PagedResult<SpaceRecord>(rawSpaces, nextCursor, hasMore));
    }
}
