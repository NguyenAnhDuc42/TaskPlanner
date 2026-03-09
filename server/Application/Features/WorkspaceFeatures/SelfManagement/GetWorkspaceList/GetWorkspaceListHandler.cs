using Application.Common.Filters;
using Application.Common.Results;
using Application.Contract.WorkspaceContract;
using Application.Features.WorkspaceFeatures.SelfManagement;
using Application.Features.WorkspaceFeatures.SelfManagement.GetWorkspaceList;
using Application.Helper;
using Application.Helpers;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using server.Application.Interfaces;
using System.Text.Json;
using Domain.Enums;
using Domain.Enums.RelationShip;


namespace Application.Features.WorkspaceFeatures.GetWorkspaceList;

public class GetWorkspaceListHandler : BaseFeatureHandler, IRequestHandler<GetWorksapceListQuery, PagedResult<WorkspaceSummaryDto>>
{
    private readonly CursorHelper _cursorHelper;
    private readonly HybridCache _cache;
    private readonly ILogger<GetWorkspaceListHandler> _logger;


    public GetWorkspaceListHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext,
        CursorHelper cursorHelper,
        HybridCache cache,
        ILogger<GetWorkspaceListHandler> logger)
        : base(unitOfWork, currentUserService, workspaceContext)
    {
        _cursorHelper = cursorHelper;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PagedResult<WorkspaceSummaryDto>> Handle(GetWorksapceListQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = CurrentUserService.CurrentUserId();
        var cacheKey = WorkspaceCacheKeys.WorkspaceList(currentUserId, request);
        _logger.LogDebug("Workspace list cache lookup. UserId={UserId}, CacheKey={CacheKey}", currentUserId, cacheKey);

        return await _cache.GetOrCreateAsync(cacheKey, async token =>
            {
                _logger.LogDebug("Workspace list cache miss. UserId={UserId}", currentUserId);
                var pageSize = request.Pagination.PageSize;

                DecodeCursor(request.Pagination.Cursor, out var cursorTs, out var cursorId);

                var sql = request.Pagination.Direction == SortDirection.Ascending
                    ? GetWorkspaceListSQL.Asc
                    : GetWorkspaceListSQL.Desc;

                var variantString = request.filter.Variant?.ToString();

                var rows = (await UnitOfWork.QueryAsync<WorkspaceRow>(sql, new
                {
                    currentUserId,
                    name = request.filter.Name,
                    owned = request.filter.Owned,
                    IsArchived = request.filter.isArchived,
                    variant = variantString,
                    cursorTimestamp = cursorTs,
                    cursorId,
                    PageSizePLusOne = pageSize + 1
                }, token)).ToList();

                _logger.LogInformation("[Diagnostic] Workspace SQL returned {Count} rows for UserId={UserId}. Filters: Name={Name}, Owned={Owned}, Archived={Archived}, Variant={Variant}", 
                    rows.Count, currentUserId, request.filter.Name, request.filter.Owned, request.filter.isArchived, variantString);

                var hasMore = rows.Count > pageSize;
                if (hasMore)
                    rows.RemoveAt(rows.Count - 1);

                var items = Map(rows);

                var nextCursor = hasMore && rows.Count > 0
                    ? EncodeNextCursor(rows[^1])
                    : null;

                return new PagedResult<WorkspaceSummaryDto>(items, nextCursor, hasMore);
            },
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) },
            new[] { $"user:{CurrentUserId}:workspaces" },
            cancellationToken);
    }

    private void DecodeCursor(string? cursor, out DateTimeOffset? ts, out Guid? id)
    {
        ts = null;
        id = null;
        if (string.IsNullOrEmpty(cursor)) return;

        var data = _cursorHelper.DecodeCursor(cursor);

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
        return rows.Select(x => new WorkspaceSummaryDto
        {
            Id = x.Id,
            Name = x.Name,
            Icon = x.Icon,
            Color = x.Color,
            Description = x.Description,
            Variant = x.Variant,
            Role = x.Role,
            MemberCount = x.MemberCount,
            IsArchived = x.IsArchived,
            IsPinned = x.IsPinned,
            CanUpdateWorkspace = x.MembershipStatus == MembershipStatus.Active && (x.Role == Role.Owner || x.Role == Role.Admin),
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

        var encoded = _cursorHelper.EncodeCursor(cursorData);

        return encoded;
    }


    //public async Task<PagedResult<WorkspaceSummaryDto>> Handle(GetWorksapceListQuery request, CancellationToken cancellationToken)
    //{
    //    var currentUserId = _currentUserService.CurrentUserId();
    //    var pageSize = request.Pagination.PageSize;

    //    var query = _unitOfWork.Set<ProjectWorkspace>()
    //        .Where(w => w.Members.Any(m => m.UserId == currentUserId));

    //    var baseQuery = query
    //        .ApplyFilter(request.filter, currentUserId)
    //        .ApplyCursor(request.Pagination, _cursorHelper)
    //        .ApplySort(request.Pagination);

    //    var rawItems = await baseQuery
    //        .Take(pageSize + 1) // fetch one extra to determine hasMore
    //        .Select(w => new    
    //        {
    //            Workspace = w,
    //            Role = w.Members.Where(m => m.UserId == currentUserId).Select(m => m.Role).Single(),
    //            MemberCount = w.Members.Count()
    //        })
    //        .AsNoTracking()
    //        .ToListAsync(cancellationToken);

    //    var hasMore = rawItems.Count > pageSize;
    //    if (hasMore) rawItems.RemoveAt(rawItems.Count - 1); // drop the extra

    //    // Map to DTOs (no UpdatedAt on DTO)
    //    var dtos = rawItems.Select(x => new WorkspaceSummaryDto
    //    {
    //        Id = x.Workspace.Id,
    //        Name = x.Workspace.Name,
    //        Icon = x.Workspace.Customization.Icon,
    //        Color = x.Workspace.Customization.Color,
    //        Description = x.Workspace.Description,
    //        Variant = x.Workspace.Variant,
    //        Role = x.Role,
    //        MemberCount = x.MemberCount
    //    }).ToList();

    //    // Build next cursor using the last workspace's UpdatedAt + Id (matches your contract)
    //    string? nextCursor = null;
    //    if (hasMore && rawItems.Count > 0)
    //    {
    //        var lastWorkspace = rawItems.Last().Workspace;
    //        nextCursor = _cursorHelper.EncodeCursor(new CursorData(new Dictionary<string, object>
    //        {
    //            { "Id", lastWorkspace.Id },
    //            { "Timestamp", lastWorkspace.UpdatedAt }
    //        }));
    //    }

    //    return new PagedResult<WorkspaceSummaryDto>(dtos, nextCursor, hasMore);
    //}

}


