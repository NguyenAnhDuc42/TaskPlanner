using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Application;

public class GetMembersHandler(
    TaskPlanDbContext db,
    WorkspaceContext context,
    CursorHelper cursorHelper,
    HybridCache cache
) : IQueryHandler<GetMembersQuery, PagedResult<MemberRecord>>
{
    public async Task<Result<PagedResult<MemberRecord>>> Handle(GetMembersQuery request, CancellationToken ct)
    {
        var workspaceId = context.workspaceId;
        var cacheKey = WorkspaceCacheKeys.MemberList(workspaceId, request);

        var result = await cache.GetOrCreateAsync(cacheKey, async token =>
        {
            var pageSize = request.pagination.PageSize;
            DecodeCursor(request.pagination.Cursor, out var cursorTimestamp, out var cursorId);

            var sql = request.pagination.Direction == SortDirection.Ascending
                ? GetMembersSQL.Asc
                : GetMembersSQL.Desc;

            var parameters = new object[]
            {
                new Npgsql.NpgsqlParameter("WorkspaceId", workspaceId),
                new Npgsql.NpgsqlParameter("name", request.filter.Name ?? (object)DBNull.Value),
                new Npgsql.NpgsqlParameter("email", request.filter.Email ?? (object)DBNull.Value),
                new Npgsql.NpgsqlParameter("role", request.filter.Role?.ToString() ?? (object)DBNull.Value),
                new Npgsql.NpgsqlParameter("cursorTimestamp", cursorTimestamp ?? (object)DBNull.Value),
                new Npgsql.NpgsqlParameter("cursorId", cursorId ?? (object)DBNull.Value),
                new Npgsql.NpgsqlParameter("pageSizePLusOne", pageSize + 1)
            };

            var rows = await db.Database.SqlQueryRaw<MemberRow>(sql, parameters).ToListAsync(token);

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
        ct);

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


