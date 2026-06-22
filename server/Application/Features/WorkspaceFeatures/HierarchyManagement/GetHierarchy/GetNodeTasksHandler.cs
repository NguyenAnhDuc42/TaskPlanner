using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Dapper;

namespace Application;

public class GetNodeTasksHandler(TaskPlanDbContext db, CursorHelper cursorHelper, WorkspaceContext workspaceContext) : IQueryHandler<GetNodeTasksQuery, PagedResult<TaskRecord>>
{
    public async Task<Result<PagedResult<TaskRecord>>> Handle(GetNodeTasksQuery request, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                t.id AS Id,
                t.name AS Name,
                t.status_id AS StatusId,
                t.priority AS Priority,
                t.order_key AS OrderKey,
                t.project_folder_id AS FolderId,
                t.project_space_id AS SpaceId,
                t.project_workspace_id AS WorkspaceId,
                t.custom_color AS Color,
                t.custom_icon AS Icon,
                CASE
                    WHEN t.project_folder_id IS NOT NULL THEN 'ProjectFolder'
                    ELSE 'ProjectSpace'
                END AS ParentType,
                (fav.entity_id IS NOT NULL) AS IsFavorite,
                fav.order_key AS FavoriteOrderKey
            FROM project_tasks t
            LEFT JOIN favorites fav
                ON fav.entity_id = t.id
                AND fav.entity_layer_type = 'ProjectTask'
                AND fav.workspace_member_id = @WorkspaceMemberId
            WHERE t.project_workspace_id = @WorkspaceId
              AND t.deleted_at IS NULL
              AND t.is_archived = false
              AND t.parent_task_id IS NULL
              AND (
                  (@ParentType = 'ProjectFolder' AND t.project_folder_id = @ParentId)
                  OR
                  (@ParentType = 'ProjectSpace' AND t.project_space_id = @ParentId AND t.project_folder_id IS NULL)
              )
              AND (
                  @CursorOrderKey::text IS NULL
                  OR t.order_key COLLATE ""C"" > @CursorOrderKey::text
                  OR (t.order_key = @CursorOrderKey::text AND t.id > @CursorTaskId::uuid)
              )
            ORDER BY t.order_key COLLATE ""C"", t.id
            LIMIT @PageSize;";
        var cursorData = request.Pagination.Cursor != null ? cursorHelper.DecodeCursor(request.Pagination.Cursor) : null;
        var cursorOrderKey = cursorData?.Values.GetValueOrDefault("OrderKey")?.ToString();
        var cursorTaskId = cursorData?.Values.GetValueOrDefault("Id")?.ToString();

        var connection = db.Database.GetDbConnection();

        var isAdmin = workspaceContext.CurrentMember.Role >= Role.Admin;
        if (!isAdmin)
        {
            var spaceId = request.ParentType == "ProjectSpace"
                ? request.ParentId
                : await connection.ExecuteScalarAsync<Guid>(
                    "SELECT project_space_id FROM project_folders WHERE id = @Id AND deleted_at IS NULL",
                    new { Id = request.ParentId });

            const string accessSql = @"
                SELECT EXISTS (
                    SELECT 1 FROM project_spaces ps
                    LEFT JOIN entity_access ea ON ea.project_space_id = ps.id
                        AND ea.workspace_member_id = @MemberId AND ea.deleted_at IS NULL
                    WHERE ps.id = @SpaceId AND ps.deleted_at IS NULL
                      AND (ps.is_private = false OR ea.id IS NOT NULL)
                )";
            var hasAccess = await connection.ExecuteScalarAsync<bool>(accessSql,
                new { SpaceId = spaceId, MemberId = workspaceContext.CurrentMember.Id });
            if (!hasAccess)
                return Result<PagedResult<TaskRecord>>.Success(new PagedResult<TaskRecord>([], null, false));
        }

        var parameters = new
        {
            WorkspaceId = request.WorkspaceId,
            ParentId = request.ParentId,
            ParentType = request.ParentType,
            CursorOrderKey = cursorOrderKey,
            CursorTaskId = cursorTaskId != null ? (Guid?)Guid.Parse(cursorTaskId) : null,
            PageSize = request.Pagination.PageSize + 1,
            WorkspaceMemberId = workspaceContext.CurrentMember.Id,
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


