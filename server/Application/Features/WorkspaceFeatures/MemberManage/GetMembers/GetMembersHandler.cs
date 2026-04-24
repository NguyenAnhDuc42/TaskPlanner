using Application.Common;
using Application.Common.Filters;
using Application.Common.Results;
using Application.Helper;
using Application.Helpers;
using Application.Interfaces;
using Application.Interfaces.Data;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Application.Features.WorkspaceFeatures;

public class GetMembersHandler(
    IDataBase db,
    WorkspaceContext context,
    CursorHelper cursorHelper,
    HybridCache cache
) : IQueryHandler<GetMembersQuery, PagedResult<MemberDto>>
{
    public async Task<Result<PagedResult<MemberDto>>> Handle(GetMembersQuery request, CancellationToken ct)
    {
        // Any workspace member can view members — PermissionDecorator guarantees context.CurrentMember
        var workspaceId = context.workspaceId;
        var cacheKey = WorkspaceCacheKeys.MemberList(workspaceId, request);

        var result = await cache.GetOrCreateAsync(cacheKey, async token =>
        {
            var pageSize = request.pagination.PageSize;
            DecodeCursor(request.pagination.Cursor, out var cursorTimestamp, out var cursorId);

            var sql = request.pagination.Direction == SortDirection.Ascending
                ? GetMembersSQL.Asc
                : GetMembersSQL.Desc;

            var rows = (await db.QueryAsync<MemberRow>(sql, new
            {
                WorkspaceId = workspaceId,
                name = request.filter.Name,
                email = request.filter.Email,
                role = request.filter.Role?.ToString(),
                cursorTimestamp,
                cursorId,
                pageSizePLusOne = pageSize + 1
            }, cancellationToken: token)).ToList();

            var hasMore = rows.Count > pageSize;
            if (hasMore) rows.RemoveAt(rows.Count - 1);

            var items = Map(rows);
            var nextCursor = hasMore && rows.Count > 0
                ? EncodeNextCursor(rows[^1])
                : null;

            return new PagedResult<MemberDto>(items, nextCursor, hasMore);
        },
        new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) },
        new[] { CacheConstants.Tags.WorkspaceMembers(workspaceId) },
        ct);

        return Result<PagedResult<MemberDto>>.Success(result);
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

    private static List<MemberDto> Map(List<MemberRow> rows)
    {
        return rows.Select(x => new MemberDto(
            x.Id,
            x.WorkspaceMemberId,
            x.Name,
            x.Email,
            x.AvatarUrl,
            x.Role,
            x.CreatedAt,
            x.JoinedAt
        )).ToList();
    }

    private string EncodeNextCursor(MemberRow last)
    {
        var cursorData = new CursorData(new Dictionary<string, object>
        {
            { "Timestamp", last.CreatedAt },
            { "Id", last.Id }
        });
        return cursorHelper.EncodeCursor(cursorData);
    }
}
