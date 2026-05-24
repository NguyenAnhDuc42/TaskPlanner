using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Dapper;

namespace Application;

public class FolderRow
{
    public Guid Id { get; init; }
    public Guid ParentId { get; init; }
    public string Name { get; init; } = null!;
    public string? Color { get; init; }
    public string? Icon { get; init; }
    public bool IsPrivate { get; init; }
    public string? OrderKey { get; init; }
    public bool? HasTasks { get; init; }
}

public class GetNodeFoldersHandler(TaskPlanDbContext db, CursorHelper cursorHelper) : IQueryHandler<GetNodeFoldersQuery, PagedResult<FolderRecord>>
{
    public async Task<Result<PagedResult<FolderRecord>>> Handle(GetNodeFoldersQuery request, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                f.id AS Id,
                f.project_space_id AS ParentId,
                f.name AS Name,
                f.custom_color AS Color,
                f.custom_icon AS Icon,
                f.is_private AS IsPrivate,
                f.order_key AS OrderKey,
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
                  OR f.order_key > @CursorOrderKey::text
                  OR (f.order_key = @CursorOrderKey::text AND f.id > @CursorFolderId::uuid)
              )
            ORDER BY f.order_key, f.id
            LIMIT @PageSize;";
        var cursorData = request.Pagination.Cursor != null ? cursorHelper.DecodeCursor(request.Pagination.Cursor) : null;
        var cursorOrderKey = cursorData?.Values.GetValueOrDefault("OrderKey")?.ToString();
        var cursorFolderId = cursorData?.Values.GetValueOrDefault("Id")?.ToString();

        var connection = db.Database.GetDbConnection();
        var parameters = new
        {
            SpaceId = request.NodeId,
            CursorOrderKey = cursorOrderKey,
            CursorFolderId = cursorFolderId != null ? (Guid?)Guid.Parse(cursorFolderId) : null,
            PageSize = request.Pagination.PageSize + 1
        };

        var rawFolders = (await connection.QueryAsync<FolderRow>(sql, parameters)).AsList();

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

        var mappedFolders = rawFolders.Select(x => new FolderRecord
        {
            Id = x.Id,
            ParentId = x.ParentId,
            Name = x.Name,
            Color = x.Color,
            Icon = x.Icon,
            IsPrivate = x.IsPrivate,
            OrderKey = x.OrderKey,
            HasTasks = x.HasTasks
        }).ToList();

        return Result<PagedResult<FolderRecord>>.Success(new PagedResult<FolderRecord>(mappedFolders, nextCursor, hasMore));
    }
}


