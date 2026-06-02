using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using NpgsqlTypes;
using Dapper;

namespace Application;

public class MemberRow
{
    public Guid Id { get; set; }
    public Guid WorkspaceMemberId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? JoinedAt { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public Role Role { get; set; }
    public MembershipStatus Status { get; set; }
}

public class GetMembersHandler(
    TaskPlanDbContext db,
    WorkspaceContext context,
    CursorHelper cursorHelper,
    HybridCache cache
) : IQueryHandler<GetMembersQuery, PagedResult<MemberRecord>>
{
    public async Task<Result<PagedResult<MemberRecord>>> Handle(GetMembersQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = context.workspaceId;
        var cacheKey = WorkspaceCacheKeys.MemberList(workspaceId, request);

        var result = await cache.GetOrCreateAsync(cacheKey, async token =>
        {
            var pageSize = request.pagination.PageSize;
            DecodeCursor(request.pagination.Cursor, out var cursorTimestamp, out var cursorId);

            var sql = request.pagination.Direction == SortDirection.Ascending
                ? @"
                SELECT 
                    u.id AS Id,
                    wm.id AS WorkspaceMemberId,
                    wm.created_at AS CreatedAt,
                    wm.joined_at AS JoinedAt,
                    u.name AS Name,
                    u.email AS Email,
                    wm.role AS Role,
                    wm.status AS Status
                FROM users u
                JOIN workspace_members wm ON wm.user_id = u.id AND wm.project_workspace_id = @WorkspaceId
                WHERE wm.deleted_at IS NULL AND
                    (@name IS NULL OR u.name ILIKE '%' || @name || '%') AND 
                    (@email IS NULL OR u.email ILIKE '%' || @email || '%') AND 
                    (@role::text IS NULL OR wm.role::text = @role) AND
                    (
                        @cursorTimestamp IS NULL OR
                            (
                                COALESCE(wm.joined_at, wm.created_at) > @cursorTimestamp OR 
                                (COALESCE(wm.joined_at, wm.created_at) = @cursorTimestamp AND u.id > @cursorId)
                            )
                    )
                ORDER BY COALESCE(wm.joined_at, wm.created_at) ASC, u.id ASC
                LIMIT @pageSizePLusOne;"
                : @"
                SELECT 
                    u.id AS Id,
                    wm.id AS WorkspaceMemberId,
                    wm.created_at AS CreatedAt,
                    wm.joined_at AS JoinedAt,
                    u.name AS Name,
                    u.email AS Email,
                    wm.role AS Role,
                    wm.status AS Status
                FROM users u
                JOIN workspace_members wm ON wm.user_id = u.id AND wm.project_workspace_id = @WorkspaceId
                WHERE wm.deleted_at IS NULL AND
                    (@name IS NULL OR u.name ILIKE '%' || @name || '%') AND 
                    (@email IS NULL OR u.email ILIKE '%' || @email || '%') AND 
                    (@role::text IS NULL OR wm.role::text = @role) AND
                    (
                        @cursorTimestamp IS NULL OR
                            (
                                COALESCE(wm.joined_at, wm.created_at) < @cursorTimestamp OR 
                                (COALESCE(wm.joined_at, wm.created_at) = @cursorTimestamp AND u.id < @cursorId)
                            )
                    )
                ORDER BY COALESCE(wm.joined_at, wm.created_at) DESC, u.id DESC
                LIMIT @pageSizePLusOne;";

            var connection = db.Database.GetDbConnection();
            var parameters = new
            {
                WorkspaceId = workspaceId,
                name = request.filter.Name,
                email = request.filter.Email,
                role = request.filter.Role?.ToString(),
                cursorTimestamp = cursorTimestamp,
                cursorId = cursorId,
                pageSizePLusOne = pageSize + 1
            };

            var rows = (await connection.QueryAsync<MemberRow>(sql, parameters)).AsList();

            var hasMore = rows.Count > pageSize;
            if (hasMore) rows.RemoveAt(rows.Count - 1);

            var items = Map(rows);
            var nextCursor = hasMore && rows.Count > 0
                ? EncodeNextCursor(rows[^1])
                : null;

            return new PagedResult<MemberRecord>(items, nextCursor, hasMore);
        },
        new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) },
        new[] { WorkspaceCacheKeys.WorkspaceMembersTag(workspaceId) },
        cancellationToken);

        return Result<PagedResult<MemberRecord>>.Success(result);
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

    private static List<MemberRecord> Map(List<MemberRow> rows)
    {
        return rows.Select(x => new MemberRecord
        {
            Id = x.Id,
            WorkspaceMemberId = x.WorkspaceMemberId,
            Name = x.Name ?? string.Empty,
            Email = x.Email,
            AvatarUrl = x.AvatarUrl,
            Role = x.Role,
            Status = x.Status,
            CreatedAt = x.CreatedAt,
            JoinedAt = x.JoinedAt
        }).ToList();
    }

    private string EncodeNextCursor(MemberRow last)
    {
        var cursorData = new CursorData(new Dictionary<string, object>
        {
            { "Timestamp", last.JoinedAt ?? last.CreatedAt },
            { "Id", last.Id }
        });
        return cursorHelper.EncodeCursor(cursorData);
    }
}


