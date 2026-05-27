using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Dapper;

namespace Application;

public class GetNodeSpacesHandler(TaskPlanDbContext db, CursorHelper cursorHelper) : IQueryHandler<GetNodeSpacesQuery, PagedResult<SpaceRecord>>
{
    public async Task<Result<PagedResult<SpaceRecord>>> Handle(GetNodeSpacesQuery request, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                id AS Id,
                project_workspace_id AS WorkspaceId,
                name AS Name,
                custom_color AS Color,
                custom_icon AS Icon,
                is_private AS IsPrivate,
                order_key AS OrderKey,
                created_at AS CreatedAt,
                EXISTS (
                    SELECT 1 FROM project_folders f
                    WHERE f.project_space_id = project_spaces.id
                      AND f.deleted_at IS NULL
                      AND f.is_archived = false
                    LIMIT 1
                ) AS HasFolders,
                EXISTS (
                    SELECT 1 FROM project_tasks t
                    WHERE t.project_space_id = project_spaces.id
                      AND t.project_folder_id IS NULL
                      AND t.deleted_at IS NULL
                      AND t.is_archived = false
                    LIMIT 1
                ) AS HasTasks
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

        var connection = db.Database.GetDbConnection();
        var parameters = new
        {
            WorkspaceId = request.WorkspaceId,
            CursorOrderKey = cursorOrderKey,
            CursorSpaceId = cursorSpaceId != null ? (Guid?)Guid.Parse(cursorSpaceId) : null,
            PageSize = request.Pagination.PageSize + 1
        };

        var mappedSpaces = (await connection.QueryAsync<SpaceRecord>(sql, parameters)).AsList();

        var hasMore = mappedSpaces.Count > request.Pagination.PageSize;
        if (hasMore) mappedSpaces.RemoveAt(mappedSpaces.Count - 1);

        var last = mappedSpaces.LastOrDefault();
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

        return Result<PagedResult<SpaceRecord>>.Success(new PagedResult<SpaceRecord>(mappedSpaces, nextCursor, hasMore));
    }
}
