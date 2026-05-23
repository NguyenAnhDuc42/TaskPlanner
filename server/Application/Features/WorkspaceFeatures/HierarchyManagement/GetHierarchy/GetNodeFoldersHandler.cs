using Microsoft.EntityFrameworkCore;

namespace Application;

public class GetNodeFoldersHandler(TaskPlanDbContext db, CursorHelper cursorHelper) : IQueryHandler<GetNodeFoldersQuery, PagedResult<FolderRecord>>
{
    public async Task<Result<PagedResult<FolderRecord>>> Handle(GetNodeFoldersQuery request, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                f.id,
                f.project_space_id AS parent_id,
                f.name,
                f.custom_color AS color,
                f.custom_icon  AS icon,
                f.is_private,
                f.order_key,
                EXISTS (
                    SELECT 1 FROM project_tasks t
                    WHERE t.project_folder_id = f.id
                      AND t.deleted_at IS NULL
                      AND t.is_archived = false
                    LIMIT 1
                ) AS has_tasks
            FROM project_folders f
            WHERE f.project_space_id = @SpaceId
              AND f.deleted_at IS NULL
              AND f.is_archived = false
              AND (
                  @CursorOrderKey::text IS NULL
                  OR f.order_key > @CursorOrderKey::text
                  OR (f.order_key = @CursorOrderKey::text AND f.id > @CursorFolderId::uuid)
              )
            ORDER BY f.order_key, f.id
            LIMIT @PageSize;";
        var cursorData = request.Pagination.Cursor != null ? cursorHelper.DecodeCursor(request.Pagination.Cursor) : null;
        var cursorOrderKey = cursorData?.Values.GetValueOrDefault("OrderKey")?.ToString();
        var cursorFolderId = cursorData?.Values.GetValueOrDefault("Id")?.ToString();

        var rawFolders = await db.Database.SqlQueryRaw<FolderRecord>(
            sql, 
            new Npgsql.NpgsqlParameter("SpaceId", request.NodeId),
            new Npgsql.NpgsqlParameter("CursorOrderKey", cursorOrderKey ?? (object)DBNull.Value),
            new Npgsql.NpgsqlParameter("CursorFolderId", cursorFolderId != null ? Guid.Parse(cursorFolderId) : (object)DBNull.Value),
            new Npgsql.NpgsqlParameter("PageSize", request.Pagination.PageSize + 1)
        ).ToListAsync(ct);

        var hasMore = rawFolders.Count > request.Pagination.PageSize;
        if (hasMore) rawFolders.RemoveAt(rawFolders.Count - 1);

        var last = rawFolders.LastOrDefault();
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

        return Result<PagedResult<FolderRecord>>.Success(new PagedResult<FolderRecord>(rawFolders, nextCursor, hasMore));
    }
}


