using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Dapper;

namespace Application;

public class GetNodeFoldersHandler(TaskPlanDbContext db, CursorHelper cursorHelper, WorkspaceContext workspaceContext) : IQueryHandler<GetNodeFoldersQuery, PagedResult<FolderRecord>>
{
    public async Task<Result<PagedResult<FolderRecord>>> Handle(GetNodeFoldersQuery request, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                f.id AS Id,
                @WorkspaceId AS WorkspaceId,
                f.project_space_id AS SpaceId,
                f.name AS Name,
                f.custom_color AS Color,
                f.custom_icon AS Icon,
                f.order_key AS OrderKey,
                f.created_at AS CreatedAt,
                EXISTS (
                    SELECT 1 FROM project_tasks t
                    WHERE t.project_folder_id = f.id
                      AND t.deleted_at IS NULL
                      AND t.is_archived = false
                    LIMIT 1
                ) AS HasTasks,
                (fav.entity_id IS NOT NULL) AS IsFavorite,
                fav.order_key AS FavoriteOrderKey,
                wf.id AS WorkflowId
            FROM project_folders f
            LEFT JOIN favorites fav
                ON fav.entity_id = f.id
                AND fav.entity_layer_type = 'ProjectFolder'
                AND fav.workspace_member_id = @WorkspaceMemberId
            LEFT JOIN workflows wf
                ON wf.project_folder_id = f.id
            WHERE f.project_space_id = @SpaceId
              AND f.deleted_at IS NULL
              AND f.is_archived = false
              AND (
                  @CursorOrderKey::text IS NULL
                  OR f.order_key COLLATE ""C"" > @CursorOrderKey::text
                  OR (f.order_key = @CursorOrderKey::text AND f.id > @CursorFolderId::uuid)
              )
            ORDER BY f.order_key COLLATE ""C"", f.id
            LIMIT @PageSize;";
        var cursorData = request.Pagination.Cursor != null ? cursorHelper.DecodeCursor(request.Pagination.Cursor) : null;
        var cursorOrderKey = cursorData?.Values.GetValueOrDefault("OrderKey")?.ToString();
        var cursorFolderId = cursorData?.Values.GetValueOrDefault("Id")?.ToString();

        var connection = db.Database.GetDbConnection();

        var isAdmin = workspaceContext.CurrentMember.Role >= Role.Admin;
        if (!isAdmin)
        {
            const string accessSql = @"
                SELECT EXISTS (
                    SELECT 1 FROM project_spaces ps
                    LEFT JOIN entity_access ea ON ea.project_space_id = ps.id
                        AND ea.workspace_member_id = @MemberId AND ea.deleted_at IS NULL
                    WHERE ps.id = @SpaceId AND ps.deleted_at IS NULL
                      AND (ps.is_private = false OR ea.id IS NOT NULL)
                )";
            var hasAccess = await connection.ExecuteScalarAsync<bool>(accessSql,
                new { SpaceId = request.NodeId, MemberId = workspaceContext.CurrentMember.Id });
            if (!hasAccess)
                return Result<PagedResult<FolderRecord>>.Success(new PagedResult<FolderRecord>([], null, false));
        }

        var parameters = new
        {
            WorkspaceId = request.WorkspaceId,
            SpaceId = request.NodeId,
            CursorOrderKey = cursorOrderKey,
            CursorFolderId = cursorFolderId != null ? (Guid?)Guid.Parse(cursorFolderId) : null,
            PageSize = request.Pagination.PageSize + 1,
            WorkspaceMemberId = workspaceContext.CurrentMember.Id,
        };

        var mappedFolders = (await connection.QueryAsync<FolderRecord>(sql, parameters)).AsList();

        var hasMore = mappedFolders.Count > request.Pagination.PageSize;
        if (hasMore) mappedFolders.RemoveAt(mappedFolders.Count - 1);

        var last = mappedFolders.LastOrDefault();
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

        return Result<PagedResult<FolderRecord>>.Success(new PagedResult<FolderRecord>(mappedFolders, nextCursor, hasMore));
    }
}


