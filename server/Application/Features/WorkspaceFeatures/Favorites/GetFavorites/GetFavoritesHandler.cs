using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Application;

public class GetFavoritesHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    CursorHelper cursorHelper
) : IQueryHandler<GetFavoritesQuery, GetFavoritesResponse>
{
    public async Task<Result<GetFavoritesResponse>> Handle(GetFavoritesQuery request, CancellationToken cancellationToken)
    {
        var conn        = db.Database.GetDbConnection();
        var memberId    = workspaceContext.CurrentMember.Id;
        var workspaceId = workspaceContext.WorkspaceId;

        var cursorData     = request.Pagination.Cursor != null ? cursorHelper.DecodeCursor(request.Pagination.Cursor) : null;
        var cursorOrderKey = cursorData?.Values.GetValueOrDefault("OrderKey")?.ToString();
        var cursorId       = cursorData?.Values.GetValueOrDefault("Id")?.ToString();

        // 1. Get the page of favorites (just IDs + type + orderKey)
        const string favSql = @"
            SELECT entity_id AS EntityId, entity_layer_type AS EntityLayerType, order_key AS OrderKey, id AS Id
            FROM favorites
            WHERE workspace_member_id  = @MemberId
              AND project_workspace_id = @WorkspaceId
              AND (
                  @CursorOrderKey::text IS NULL
                  OR order_key COLLATE ""C"" > @CursorOrderKey::text
                  OR (order_key = @CursorOrderKey::text AND id > @CursorId::uuid)
              )
            ORDER BY order_key COLLATE ""C"", id
            LIMIT @PageSize;";

        var favRows = (await conn.QueryAsync<(Guid EntityId, string EntityLayerType, string OrderKey, Guid Id)>(favSql, new
        {
            MemberId     = memberId,
            WorkspaceId  = workspaceId,
            CursorOrderKey = cursorOrderKey,
            CursorId     = cursorId != null ? (Guid?)Guid.Parse(cursorId) : null,
            PageSize     = request.Pagination.PageSize + 1,
        })).AsList();

        var hasMore = favRows.Count > request.Pagination.PageSize;
        if (hasMore) favRows.RemoveAt(favRows.Count - 1);

        var orderKeyMap = favRows.ToDictionary(r => r.EntityId, r => r.OrderKey);

        // 2. Fetch full entity records per type
        var spaceIds  = favRows.Where(r => r.EntityLayerType == "ProjectSpace").Select(r => r.EntityId).ToArray();
        var folderIds = favRows.Where(r => r.EntityLayerType == "ProjectFolder").Select(r => r.EntityId).ToArray();
        var taskIds   = favRows.Where(r => r.EntityLayerType == "ProjectTask").Select(r => r.EntityId).ToArray();

        List<SpaceRecord>  spaces  = [];
        List<FolderRecord> folders = [];
        List<TaskRecord>   tasks   = [];

        if (spaceIds.Length > 0)
        {
            var rows = await conn.QueryAsync<SpaceRecord>(@"
                SELECT id AS Id, project_workspace_id AS WorkspaceId, name AS Name,
                       custom_color AS Color, custom_icon AS Icon, is_private AS IsPrivate,
                       order_key AS OrderKey
                FROM project_spaces WHERE id = ANY(@Ids) AND deleted_at IS NULL",
                new { Ids = spaceIds });

            spaces = rows.Select(r => r with { IsFavorite = true, FavoriteOrderKey = orderKeyMap.GetValueOrDefault(r.Id) }).AsList();
        }

        if (folderIds.Length > 0)
        {
            var rows = await conn.QueryAsync<FolderRecord>(@"
                SELECT f.id AS Id, @WorkspaceId AS WorkspaceId, f.project_space_id AS SpaceId,
                       f.name AS Name, f.start_date AS StartDate, f.due_date AS DueDate,
                       f.order_key AS OrderKey, f.custom_icon AS Icon, f.custom_color AS Color
                FROM project_folders f WHERE f.id = ANY(@Ids) AND f.deleted_at IS NULL",
                new { WorkspaceId = workspaceId, Ids = folderIds });

            folders = rows.Select(r => r with { IsFavorite = true, FavoriteOrderKey = orderKeyMap.GetValueOrDefault(r.Id) }).AsList();
        }

        if (taskIds.Length > 0)
        {
            var rows = await conn.QueryAsync<TaskRecord>(@"
                SELECT t.id AS Id, @WorkspaceId AS WorkspaceId, t.project_space_id AS SpaceId,
                       t.project_folder_id AS FolderId, t.name AS Name, t.status_id AS StatusId,
                       t.priority AS Priority, t.due_date AS DueDate, t.start_date AS StartDate,
                       t.order_key AS OrderKey, t.custom_icon AS Icon, t.custom_color AS Color,
                       t.parent_task_id AS ParentTaskId
                FROM project_tasks t WHERE t.id = ANY(@Ids) AND t.deleted_at IS NULL",
                new { WorkspaceId = workspaceId, Ids = taskIds });

            tasks = rows.Select(r => r with { IsFavorite = true, FavoriteOrderKey = orderKeyMap.GetValueOrDefault(r.Id) }).AsList();
        }

        string? nextCursor = null;
        if (hasMore && favRows.Count > 0)
        {
            var last = favRows[^1];
            nextCursor = cursorHelper.EncodeCursor(new CursorData(new Dictionary<string, object>
            {
                { "OrderKey", last.OrderKey },
                { "Id", last.Id.ToString() }
            }));
        }

        return Result<GetFavoritesResponse>.Success(new GetFavoritesResponse(spaces, folders, tasks, nextCursor, hasMore));
    }
}
