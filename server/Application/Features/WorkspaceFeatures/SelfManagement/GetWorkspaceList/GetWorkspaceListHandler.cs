using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using NpgsqlTypes;
using Dapper;

namespace Application;

public class WorkspaceRow
{
    public Guid Id { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string Name { get; init; } = null!;
    public string Icon { get; init; } = null!;
    public string Color { get; init; } = null!;
    public string Description { get; init; } = null!;
    public Role Role { get; init; }
    public MembershipStatus MembershipStatus { get; init; }
    public int MemberCount { get; init; }
    public bool IsArchived { get; init; }
    public bool IsPinned { get; init; }
    public string? MembersJson { get; init; }
}

public class GetWorkspaceListHandler(
    TaskPlanDbContext db,
    CurrentUserService currentUserService,
    CursorHelper cursorHelper,
    HybridCache cache
) : IQueryHandler<GetWorksapceListQuery, PagedResult<WorkspaceRecord>>
{
    public async Task<Result<PagedResult<WorkspaceRecord>>> Handle(GetWorksapceListQuery request, CancellationToken ct)
    {
        var currentUserId = currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) 
            return Result<PagedResult<WorkspaceRecord>>.Failure(UserError.NotFound);

        var cacheKey = WorkspaceCacheKeys.WorkspaceList(currentUserId, request);
        
        var result = await cache.GetOrCreateAsync(cacheKey, async token =>
        {
            var pageSize = request.Pagination.PageSize;

            DecodeCursor(request.Pagination.Cursor, out var cursorTs, out var cursorId);

            var sql = request.Pagination.Direction == SortDirection.Ascending
                ? @"
                SELECT w.id AS Id, w.updated_at AS UpdatedAt, w.name AS Name, w.custom_icon AS Icon, w.custom_color AS Color,
                       w.description AS Description, w.is_archived AS IsArchived, wm.role AS Role, wm.status AS MembershipStatus, wm.is_pinned AS IsPinned,
                       (SELECT COUNT(*) FROM workspace_members WHERE project_workspace_id = w.id AND deleted_at IS NULL) AS MemberCount,
                       (SELECT json_agg(json_build_object('Id', u.id, 'Name', u.name, 'Role', m.role))
                        FROM (SELECT user_id, role FROM workspace_members WHERE project_workspace_id = w.id AND deleted_at IS NULL ORDER BY created_at ASC LIMIT 5) m
                        JOIN users u ON u.id = m.user_id) AS MembersJson
                FROM project_workspaces w
                JOIN workspace_members wm ON wm.project_workspace_id = w.id AND wm.user_id = @CurrentUserId AND wm.deleted_at IS NULL
                WHERE w.deleted_at IS NULL AND
                      (@name IS NULL OR w.name ILIKE '%' || @name || '%') AND 
                      (@owned IS NULL OR @owned = false OR wm.role = 'Owner') AND
                      (@isArchived IS NULL OR w.is_archived = @isArchived) AND 
                      (@cursorTimestamp IS NULL OR (w.updated_at > @cursorTimestamp OR (w.updated_at = @cursorTimestamp AND w.id > @cursorId)))
                ORDER BY wm.is_pinned DESC, w.updated_at ASC, w.id ASC
                LIMIT @PageSizePLusOne;"
                : @"
                SELECT w.id AS Id, w.updated_at AS UpdatedAt, w.name AS Name, w.custom_icon AS Icon, w.custom_color AS Color,
                       w.description AS Description, w.is_archived AS IsArchived, wm.role AS Role, wm.status AS MembershipStatus, wm.is_pinned AS IsPinned,
                       (SELECT COUNT(*) FROM workspace_members WHERE project_workspace_id = w.id AND deleted_at IS NULL) AS MemberCount,
                       (SELECT json_agg(json_build_object('Id', u.id, 'Name', u.name, 'Role', m.role))
                        FROM (SELECT user_id, role FROM workspace_members WHERE project_workspace_id = w.id AND deleted_at IS NULL ORDER BY created_at ASC LIMIT 5) m
                        JOIN users u ON u.id = m.user_id) AS MembersJson
                FROM project_workspaces w
                JOIN workspace_members wm ON wm.project_workspace_id = w.id AND wm.user_id = @CurrentUserId AND wm.deleted_at IS NULL
                WHERE w.deleted_at IS NULL AND
                      (@name IS NULL OR w.name ILIKE '%' || @name || '%') AND 
                      (@owned IS NULL OR @owned = false OR wm.role = 'Owner') AND
                      (@isArchived IS NULL OR w.is_archived = @isArchived) AND 
                      (@cursorTimestamp IS NULL OR (w.updated_at < @cursorTimestamp OR (w.updated_at = @cursorTimestamp AND w.id < @cursorId)))
                ORDER BY wm.is_pinned DESC, w.updated_at DESC, w.id DESC
                LIMIT @PageSizePLusOne;";

            var connection = db.Database.GetDbConnection();
            var parameters = new
            {
                CurrentUserId = currentUserId,
                name = request.filter.Name,
                owned = request.filter.Owned,
                isArchived = request.filter.isArchived,
                cursorTimestamp = cursorTs,
                cursorId = cursorId,
                PageSizePLusOne = pageSize + 1
            };

            var rows = (await connection.QueryAsync<WorkspaceRow>(sql, parameters)).AsList();

            var hasMore = rows.Count > pageSize;
            if (hasMore) rows.RemoveAt(rows.Count - 1);

            var items = Map(rows);

            var nextCursor = hasMore && rows.Count > 0
                ? EncodeNextCursor(rows[^1])
                : null;

            return new PagedResult<WorkspaceRecord>(items, nextCursor, hasMore);
        },
        new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) },
        new[] { $"user:{currentUserId}:workspaces" },
        ct);

        return Result<PagedResult<WorkspaceRecord>>.Success(result);
    }

    private void DecodeCursor(string? cursor, out DateTimeOffset? ts, out Guid? id)
    {
        ts = null;
        id = null;
        if (string.IsNullOrEmpty(cursor)) return;

        var data = cursorHelper.DecodeCursor(cursor);
        if (data?.Values == null) return;

        if (data.Values.TryGetValue("Timestamp", out var tsObj))
        {
            var tsStr = tsObj is JsonElement tsElement ? tsElement.GetString() : tsObj?.ToString();
            if (DateTimeOffset.TryParse(tsStr, out var parsedTs))
                ts = parsedTs;
        }
        if (data.Values.TryGetValue("Id", out var idObj))
        {
            var idStr = idObj is JsonElement idElement ? idElement.GetString() : idObj?.ToString();
            if (Guid.TryParse(idStr, out var parsedId))
                id = parsedId;
        }
    }

    private static List<WorkspaceRecord> Map(List<WorkspaceRow> rows)
    {
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return rows.Select(x => new WorkspaceRecord
        {
            Id = x.Id,
            Name = x.Name,
            Icon = x.Icon,
            Color = x.Color,
            Description = x.Description,
            Role = x.Role,
            MemberCount = x.MemberCount,
            IsArchived = x.IsArchived,
            IsPinned = x.IsPinned,
            CanEdit = x.MembershipStatus == MembershipStatus.Active && (x.Role == Role.Owner || x.Role == Role.Admin),
            CanManageMembers = x.MembershipStatus == MembershipStatus.Active && (x.Role == Role.Owner || x.Role == Role.Admin),
            CanPinWorkspace = x.MembershipStatus == MembershipStatus.Active
        }).ToList();
    }

    private string EncodeNextCursor(WorkspaceRow last)
    {
        var cursorData = new CursorData(new Dictionary<string, object>
        {
            { "Timestamp", last.UpdatedAt },
            { "Id", last.Id }
        });

        return cursorHelper.EncodeCursor(cursorData);
    }
}


