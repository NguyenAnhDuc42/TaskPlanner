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
                ) AS HasTasks
            FROM project_folders f
            WHERE f.project_space_id = @SpaceId
              AND f.deleted_at IS NULL
              AND f.is_archived = false
              AND (
                  @CursorOrderKey::text IS NULL
                  OR f.order_key COLLATE ""C"" > @CursorOrderKey::text
                  OR (f.order_key = @CursorOrderKey::text AND f.id > @CursorFolderId::uuid)
              )
              AND (
                  @Role >= @AdminRole
                  OR EXISTS (
                      SELECT 1 FROM project_spaces ps
                      WHERE ps.id = f.project_space_id
                      AND (
                          ps.is_private = false
                          OR EXISTS (
                              SELECT 1 FROM entity_access ea
                              WHERE ea.project_space_id = ps.id
                              AND ea.workspace_member_id = @WorkspaceMemberId
                              AND ea.access_level = ANY(@ValidAccessLevels)
                              AND ea.deleted_at IS NULL
                          )
                      )
                  )
              )
            ORDER BY f.order_key COLLATE ""C"", f.id
            LIMIT @PageSize;";
        var cursorData = request.Pagination.Cursor != null ? cursorHelper.DecodeCursor(request.Pagination.Cursor) : null;
        var cursorOrderKey = cursorData?.Values.GetValueOrDefault("OrderKey")?.ToString();
        var cursorFolderId = cursorData?.Values.GetValueOrDefault("Id")?.ToString();

        var connection = db.Database.GetDbConnection();
        var parameters = new
        {
            WorkspaceId = request.WorkspaceId,
            SpaceId = request.NodeId,
            CursorOrderKey = cursorOrderKey,
            CursorFolderId = cursorFolderId != null ? (Guid?)Guid.Parse(cursorFolderId) : null,
            PageSize = request.Pagination.PageSize + 1,
            Role = (int)workspaceContext.CurrentMember.Role,
            AdminRole = (int)Role.Admin,
            WorkspaceMemberId = workspaceContext.CurrentMember.Id,
            ValidAccessLevels = new[] { "Viewer", "Editor", "Manager" }
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


