using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Application;

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
                ? GetWorkspaceListSQL.Asc
                : GetWorkspaceListSQL.Desc;

            var parameters = new object[]
            {
                new Npgsql.NpgsqlParameter("CurrentUserId", currentUserId),
                new Npgsql.NpgsqlParameter("name", request.filter.Name ?? (object)DBNull.Value),
                new Npgsql.NpgsqlParameter("owned", request.filter.Owned ?? (object)DBNull.Value),
                new Npgsql.NpgsqlParameter("isArchived", request.filter.isArchived ?? (object)DBNull.Value),
                new Npgsql.NpgsqlParameter("cursorTimestamp", cursorTs ?? (object)DBNull.Value),
                new Npgsql.NpgsqlParameter("cursorId", cursorId ?? (object)DBNull.Value),
                new Npgsql.NpgsqlParameter("PageSizePLusOne", pageSize + 1)
            };

            var rows = await db.Database.SqlQueryRaw<WorkspaceRow>(sql, parameters).ToListAsync(token);

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
            CanPinWorkspace = x.MembershipStatus == MembershipStatus.Active,
            Members = string.IsNullOrEmpty(x.MembersJson) 
                ? new List<MemberRecord>() 
                : JsonSerializer.Deserialize<List<MemberRecord>>(x.MembersJson, jsonOptions) ?? new()
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



