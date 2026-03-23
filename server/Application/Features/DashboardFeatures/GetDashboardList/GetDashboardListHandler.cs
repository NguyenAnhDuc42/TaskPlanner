using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features.DashboardFeatures;
using Application.Helper;
using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using server.Application.Interfaces;


namespace Application.Features.DashboardFeatures.GetDashboardList;

public class GetDashboardListHandler : BaseFeatureHandler, IRequestHandler<GetDashboardListQuery, PagedResult<DashboardDto>>
{
    private readonly HybridCache _cache;
    private readonly CursorHelper _cursorHelper;

    public GetDashboardListHandler(
        IUnitOfWork unitOfWork, 
        ICurrentUserService currentUserService, 
        WorkspaceContext workspaceContext, 
        HybridCache cache,
        CursorHelper cursorHelper)
        : base(unitOfWork, currentUserService, workspaceContext)
    {
        _cache = cache;
        _cursorHelper = cursorHelper;
    }

    public async Task<PagedResult<DashboardDto>> Handle(GetDashboardListQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = DashboardCacheKeys.DashboardList(request.layerId);
        
        return await _cache.GetOrCreateAsync(
            $"{cacheKey}:{request.Pagination.Cursor}:{request.Pagination.PageSize}",
            async ct => 
            {
                var pageSize = request.Pagination.PageSize;
                var query = UnitOfWork.Set<Dashboard>()
                    .Where(d => d.LayerId == request.layerId && d.LayerType == request.layerType && d.DeletedAt == null);

                // Apply Cursor
                if (!string.IsNullOrEmpty(request.Pagination.Cursor))
                {
                    var data = _cursorHelper.DecodeCursor(request.Pagination.Cursor);
                    if (data?.Values != null)
                    {
                        if (data.Values.TryGetValue("UpdatedAt", out var tsObj) && data.Values.TryGetValue("Id", out var idObj))
                        {
                            var ts = DateTimeOffset.Parse(tsObj.ToString()!);
                            var id = Guid.Parse(idObj.ToString()!);
                            query = query.Where(d => d.UpdatedAt < ts || (d.UpdatedAt == ts && d.Id < id));
                        }
                    }
                }

                var itemsWithExtra = await query
                    .OrderByDescending(d => d.IsMain)
                    .ThenByDescending(d => d.UpdatedAt)
                    .ThenByDescending(d => d.Id)
                    .Select(d => new DashboardDto(
                        d.Id,
                        d.Name,
                        d.IsShared,
                        d.IsMain,
                        d.LayerType,
                        d.LayerId,
                        d.UpdatedAt))
                    .AsNoTracking()
                    .Take(pageSize + 1)
                    .ToListAsync(ct);

                var hasMore = itemsWithExtra.Count > pageSize;
                var items = itemsWithExtra.Take(pageSize).ToList();

                string? nextCursor = null;
                if (hasMore && items.Count > 0)
                {
                    var last = items[^1];
                    nextCursor = _cursorHelper.EncodeCursor(new CursorData(new Dictionary<string, object>
                    {
                        { "UpdatedAt", last.UpdatedAt },
                        { "Id", last.Id }
                    }));
                }

                return new PagedResult<DashboardDto>(items, nextCursor, hasMore);
            },
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) },
            new[] { DashboardCacheKeys.DashboardListTag(request.layerId) },
            cancellationToken);
    }
}
