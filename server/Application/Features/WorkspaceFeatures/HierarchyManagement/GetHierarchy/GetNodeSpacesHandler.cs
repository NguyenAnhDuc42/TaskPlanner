using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Dapper;

namespace Application;

public class GetNodeSpacesHandler(TaskPlanDbContext db, CursorHelper cursorHelper, WorkspaceContext workspaceContext) : IQueryHandler<GetNodeSpacesQuery, PagedResult<SpaceRecord>>
{
    public async Task<Result<PagedResult<SpaceRecord>>> Handle(GetNodeSpacesQuery request, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                ps.id AS Id,
                ps.project_workspace_id AS WorkspaceId,
                ps.name AS Name,
                ps.custom_color AS Color,
                ps.custom_icon AS Icon,
                ps.is_private AS IsPrivate,
                ps.order_key AS OrderKey,
                ps.created_at AS CreatedAt,
                EXISTS (
                    SELECT 1 FROM project_folders f
                    WHERE f.project_space_id = ps.id
                      AND f.deleted_at IS NULL
                      AND f.is_archived = false
                    LIMIT 1
                ) AS HasFolders,
                EXISTS (
                    SELECT 1 FROM project_tasks t
                    WHERE t.project_space_id = ps.id
                      AND t.project_folder_id IS NULL
                      AND t.deleted_at IS NULL
                      AND t.is_archived = false
                    LIMIT 1
                ) AS HasTasks,
                (fav.entity_id IS NOT NULL) AS IsFavorite,
                fav.order_key AS FavoriteOrderKey
            FROM project_spaces ps
            LEFT JOIN favorites fav
                ON fav.entity_id = ps.id
                AND fav.entity_layer_type = 'ProjectSpace'
                AND fav.workspace_member_id = @WorkspaceMemberId
            WHERE ps.project_workspace_id = @WorkspaceId
              AND ps.deleted_at IS NULL
              AND ps.is_archived = false
              AND (
                  @CursorOrderKey::text IS NULL
                  OR ps.order_key COLLATE ""C"" > @CursorOrderKey::text
                  OR (ps.order_key = @CursorOrderKey::text AND ps.id > @CursorSpaceId::uuid)
              )
              AND (
                  @Role >= @AdminRole
                  OR ps.is_private = false
                  OR EXISTS (
                      SELECT 1 FROM entity_access ea
                      WHERE ea.project_space_id = ps.id
                      AND ea.workspace_member_id = @WorkspaceMemberId
                      AND ea.deleted_at IS NULL
                  )
              )
            ORDER BY ps.order_key COLLATE ""C"", ps.id
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
            PageSize = request.Pagination.PageSize + 1,
            Role = (int)workspaceContext.CurrentMember.Role,
            AdminRole = (int)Role.Admin,
            WorkspaceMemberId = workspaceContext.CurrentMember.Id
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
