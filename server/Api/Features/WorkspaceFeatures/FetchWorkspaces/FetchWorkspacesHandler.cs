using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Dapper;

namespace Api;

public class FetchWorkspacesRow
{
    public Guid Id { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string Name { get; init; } = null!;
    public string Icon { get; init; } = null!;
    public string Color { get; init; } = null!;
    public string Description { get; init; } = null!;
    public Role Role { get; init; }
    public MembershipStatus MembershipStatus { get; init; }
    public bool IsArchived { get; init; }
    public bool IsPinned { get; init; }
    public string? JoinCode { get; init; }
    public bool StrictJoin { get; init; }
}

public class FetchWorkspacesHandler(
    TaskPlanDbContext db,
    CurrentUserService currentUserService,
    CursorHelper cursorHelper
) : IQueryHandler<FetchWorkspacesQuery, PagedResult<WorkspaceRecord>>
{
    public async Task<Result<PagedResult<WorkspaceRecord>>> Handle(FetchWorkspacesQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty)
            return Result<PagedResult<WorkspaceRecord>>.Failure(UserError.NotFound);

        {
            var pageSize = request.Pagination.PageSize;

            DecodeCursor(request.Pagination.Cursor, out var cursorTs, out var cursorId);

            var sql = request.Pagination.Direction == SortDirection.Ascending
                ? @"
                SELECT w.id AS Id, w.updated_at AS UpdatedAt, w.name AS Name, w.custom_icon AS Icon, w.custom_color AS Color,
                       w.description AS Description, w.is_archived AS IsArchived, wm.role AS Role, wm.status AS MembershipStatus, wm.is_pinned AS IsPinned,
                       w.join_code AS JoinCode, w.strict_join AS StrictJoin
                FROM project_workspaces w
                JOIN workspace_members wm ON wm.project_workspace_id = w.id AND wm.user_id = @CurrentUserId AND wm.deleted_at IS NULL
                WHERE w.deleted_at IS NULL AND
                      (@name IS NULL OR w.name ILIKE '%' || @name || '%') AND
                      (@owned IS NULL OR @owned = false OR wm.role = 'Owner') AND
                      (@isArchived IS NULL OR w.is_archived = @isArchived) AND
                      (@cursorTimestamp IS NULL OR (w.updated_at > @cursorTimestamp OR (w.updated_at = @cursorTimestamp AND w.id > @cursorId)))
                ORDER BY wm.is_pinned DESC, w.updated_at ASC, w.id ASC
                LIMIT @PageSizePlusOne;"
                : @"
                SELECT w.id AS Id, w.updated_at AS UpdatedAt, w.name AS Name, w.custom_icon AS Icon, w.custom_color AS Color,
                       w.description AS Description, w.is_archived AS IsArchived, wm.role AS Role, wm.status AS MembershipStatus, wm.is_pinned AS IsPinned,
                       w.join_code AS JoinCode, w.strict_join AS StrictJoin
                FROM project_workspaces w
                JOIN workspace_members wm ON wm.project_workspace_id = w.id AND wm.user_id = @CurrentUserId AND wm.deleted_at IS NULL
                WHERE w.deleted_at IS NULL AND
                      (@name IS NULL OR w.name ILIKE '%' || @name || '%') AND
                      (@owned IS NULL OR @owned = false OR wm.role = 'Owner') AND
                      (@isArchived IS NULL OR w.is_archived = @isArchived) AND
                      (@cursorTimestamp IS NULL OR (w.updated_at < @cursorTimestamp OR (w.updated_at = @cursorTimestamp AND w.id < @cursorId)))
                ORDER BY wm.is_pinned DESC, w.updated_at DESC, w.id DESC
                LIMIT @PageSizePlusOne;";

            var connection = db.Database.GetDbConnection();
            var parameters = new
            {
                CurrentUserId = currentUserId,
                name = request.Filter.Name,
                owned = request.Filter.Owned,
                isArchived = request.Filter.IsArchived,
                cursorTimestamp = cursorTs,
                cursorId = cursorId,
                PageSizePlusOne = pageSize + 1
            };

            var rows = (await connection.QueryAsync<FetchWorkspacesRow>(sql, parameters)).AsList();

            var hasMore = rows.Count > pageSize;
            if (hasMore) rows.RemoveAt(rows.Count - 1);

            var items = Map(rows);

            var nextCursor = hasMore && rows.Count > 0
                ? EncodeNextCursor(rows[^1])
                : null;

            var result = new PagedResult<WorkspaceRecord>(items, nextCursor, hasMore);
            return Result<PagedResult<WorkspaceRecord>>.Success(result);
        }
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

    private static List<WorkspaceRecord> Map(List<FetchWorkspacesRow> rows)
    {
        return rows.Select(x => new WorkspaceRecord
        {
            Id = x.Id,
            Name = x.Name,
            Icon = x.Icon,
            Color = x.Color,
            Description = x.Description,
            IsArchived = x.IsArchived,
            Role = x.Role,
            IsPinned = x.IsPinned,
            MembershipStatus = x.MembershipStatus,
            JoinCode = (x.Role == Role.Owner || x.Role == Role.Admin) ? x.JoinCode : null,
            StrictJoin = x.StrictJoin,
        }).ToList();
    }

    private string EncodeNextCursor(FetchWorkspacesRow last)
    {
        var cursorData = new CursorData(new Dictionary<string, object>
        {
            { "Timestamp", last.UpdatedAt },
            { "Id", last.Id }
        });

        return cursorHelper.EncodeCursor(cursorData);
    }
}
