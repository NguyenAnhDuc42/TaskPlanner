using System.Text.Json;
using Application.Common.Filters;
using Application.Common.Results;
using Application.Common.Errors;
using Application.Helper;
using Application.Helpers;
using Application.Interfaces;
using Application.Interfaces.Data;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Application.Features.WorkspaceFeatures;

public class GetWorkspaceListHandler(
    IDataBase db,
    ICurrentUserService currentUserService,
    CursorHelper cursorHelper,
    HybridCache cache
) : IQueryHandler<GetWorksapceListQuery, PagedResult<WorkspaceSummaryDto>>
{
    public async Task<Result<PagedResult<WorkspaceSummaryDto>>> Handle(GetWorksapceListQuery request, CancellationToken ct)
    {
        var currentUserId = currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) 
            return Result<PagedResult<WorkspaceSummaryDto>>.Failure(UserError.NotFound);

        var cacheKey = WorkspaceCacheKeys.WorkspaceList(currentUserId, request);
        
        var result = await cache.GetOrCreateAsync(cacheKey, async token =>
        {
            var pageSize = request.Pagination.PageSize;

            DecodeCursor(request.Pagination.Cursor, out var cursorTs, out var cursorId);

            var sql = request.Pagination.Direction == SortDirection.Ascending
                ? GetWorkspaceListSQL.Asc
                : GetWorkspaceListSQL.Desc;

            var rows = (await db.QueryAsync<WorkspaceRow>(sql, new
            {
                currentUserId,
                name = request.filter.Name,
                owned = request.filter.Owned,
                IsArchived = request.filter.isArchived,
                cursorTimestamp = cursorTs,
                cursorId,
                PageSizePLusOne = pageSize + 1
            }, cancellationToken: token)).ToList();

            var hasMore = rows.Count > pageSize;
            if (hasMore) rows.RemoveAt(rows.Count - 1);

            var items = Map(rows);

            var nextCursor = hasMore && rows.Count > 0
                ? EncodeNextCursor(rows[^1])
                : null;

            return new PagedResult<WorkspaceSummaryDto>(items, nextCursor, hasMore);
        },
        new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) },
        new[] { $"user:{currentUserId}:workspaces" },
        ct);

        return Result<PagedResult<WorkspaceSummaryDto>>.Success(result);
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

    private static List<WorkspaceSummaryDto> Map(List<WorkspaceRow> rows)
    {
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return rows.Select(x => new WorkspaceSummaryDto(
            Id: x.Id,
            Name: x.Name,
            Icon: x.Icon,
            Color: x.Color,
            Description: x.Description,
            Role: x.Role,
            MemberCount: x.MemberCount,
            IsArchived: x.IsArchived,
            IsPinned: x.IsPinned,
            CanUpdateWorkspace: x.MembershipStatus == MembershipStatus.Active && (x.Role == Role.Owner || x.Role == Role.Admin),
            CanManageMembers: x.MembershipStatus == MembershipStatus.Active && (x.Role == Role.Owner || x.Role == Role.Admin),
            CanPinWorkspace: x.MembershipStatus == MembershipStatus.Active,
            Members: string.IsNullOrEmpty(x.MembersJson) 
                ? new List<WorkspaceMemberSummaryDto>() 
                : JsonSerializer.Deserialize<List<WorkspaceMemberSummaryDto>>(x.MembersJson, jsonOptions) ?? new()
        )).ToList();
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
