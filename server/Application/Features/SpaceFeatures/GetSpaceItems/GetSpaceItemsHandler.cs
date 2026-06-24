using Dapper;
using Microsoft.EntityFrameworkCore;
namespace Application;

public class GetSpaceItemsHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext, CursorHelper cursorHelper)
    : IQueryHandler<GetSpaceItemsQuery, GetSpaceItemsResponse>
{
    public async Task<Result<GetSpaceItemsResponse>> Handle(GetSpaceItemsQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = workspaceContext.WorkspaceId;
        var memberId    = workspaceContext.CurrentMember.Id;
        var isOwner     = workspaceContext.CurrentMember.Role == Domain.Role.Owner;
        var connection  = db.Database.GetDbConnection();

        var hasAccess = await connection.ExecuteScalarAsync<bool>(@"
            SELECT 1 FROM project_spaces s
            LEFT JOIN entity_access ea ON ea.project_space_id = s.id
                AND ea.workspace_member_id = @MemberId AND ea.deleted_at IS NULL
            WHERE s.id = @SpaceId AND s.project_workspace_id = @WorkspaceId AND s.deleted_at IS NULL
              AND (s.is_private = false OR (ea.id IS NOT NULL AND ea.access_level IN ('Viewer', 'Editor', 'Manager')) OR @IsOwner = true);",
            new { SpaceId = request.SpaceId, WorkspaceId = workspaceId, MemberId = memberId, IsOwner = isOwner });

        if (!hasAccess)
            return Result<GetSpaceItemsResponse>.Failure(Error.NotFound("Space.NotFound", "Space not found or access denied"));

        var cursorData     = request.Cursor != null ? cursorHelper.DecodeCursor(request.Cursor) : null;
        var cursorOrderKey = cursorData?.Values.GetValueOrDefault("OrderKey")?.ToString();
        var cursorId       = cursorData?.Values.GetValueOrDefault("Id")?.ToString();
        var isFirstPage    = cursorData == null;

        // Statuses and folders only on first page — subsequent pages are tasks only
        List<StatusRecord> statuses = [];
        List<FolderRecord> folders  = [];

        if (isFirstPage)
        {
            const string metaSql = @"
                SELECT id AS Id, project_space_id AS SpaceId, name AS Name, color AS Color, category AS Category, order_key AS OrderKey
                FROM statuses
                WHERE project_space_id = @SpaceId
                ORDER BY CASE category
                    WHEN 'NotStarted' THEN 0 WHEN 'Active' THEN 1
                    WHEN 'Done' THEN 2 WHEN 'Closed' THEN 3 ELSE 4 END;

                SELECT f.id AS Id, @WorkspaceId AS WorkspaceId, f.project_space_id AS SpaceId,
                       f.name AS Name, f.created_at AS CreatedAt,
                       f.start_date AS StartDate, f.due_date AS DueDate,
                       f.order_key AS OrderKey, f.custom_icon AS Icon, f.custom_color AS Color,
                       CAST(CASE WHEN fav.id IS NOT NULL THEN 1 ELSE 0 END AS BOOLEAN) AS IsFavorite
                FROM project_folders f
                LEFT JOIN favorites fav ON fav.entity_id = f.id AND fav.workspace_member_id = @MemberId
                WHERE f.project_space_id = @SpaceId AND f.deleted_at IS NULL AND f.is_archived = false
                ORDER BY f.order_key;";

            using var meta = await connection.QueryMultipleAsync(metaSql, new { SpaceId = request.SpaceId, WorkspaceId = workspaceId, MemberId = memberId });
            statuses = (await meta.ReadAsync<StatusRecord>()).AsList();
            folders  = (await meta.ReadAsync<FolderRecord>()).AsList();
        }

        var tasks = (await connection.QueryAsync<TaskRecord>(@"
            SELECT t.id AS Id, @WorkspaceId AS WorkspaceId, t.project_space_id AS SpaceId,
                   t.project_folder_id AS FolderId, t.name AS Name, t.created_at AS CreatedAt,
                   t.status_id AS StatusId, t.priority AS Priority,
                   t.due_date AS DueDate, t.start_date AS StartDate,
                   t.order_key AS OrderKey, t.custom_icon AS Icon, t.custom_color AS Color,
                   t.parent_task_id AS ParentTaskId,
                   CAST(CASE WHEN fav.id IS NOT NULL THEN 1 ELSE 0 END AS BOOLEAN) AS IsFavorite
            FROM project_tasks t
            LEFT JOIN favorites fav ON fav.entity_id = t.id AND fav.workspace_member_id = @MemberId
            WHERE t.project_workspace_id = @WorkspaceId
              AND t.project_space_id = @SpaceId
              AND t.deleted_at IS NULL AND t.is_archived = false
              AND t.parent_task_id IS NULL
              AND (
                  @CursorOrderKey::text IS NULL
                  OR t.order_key COLLATE ""C"" > @CursorOrderKey::text
                  OR (t.order_key = @CursorOrderKey::text AND t.id > @CursorId::uuid)
              )
            ORDER BY t.order_key COLLATE ""C"", t.id
            LIMIT @Limit;",
            new
            {
                WorkspaceId    = workspaceId,
                SpaceId        = request.SpaceId,
                MemberId       = memberId,
                CursorOrderKey = cursorOrderKey,
                CursorId       = cursorId != null ? (Guid?)Guid.Parse(cursorId) : null,
                Limit          = request.Limit + 1,
            })).AsList();

        var hasMore = tasks.Count > request.Limit;
        if (hasMore) tasks.RemoveAt(tasks.Count - 1);

        string? nextCursor = null;
        if (hasMore)
        {
            var last = tasks[^1];
            nextCursor = cursorHelper.EncodeCursor(new CursorData(new Dictionary<string, object>
            {
                { "OrderKey", last.OrderKey ?? string.Empty },
                { "Id", last.Id.ToString() }
            }));
        }

        return Result<GetSpaceItemsResponse>.Success(new GetSpaceItemsResponse(folders, tasks, statuses, hasMore, nextCursor));
    }
}
