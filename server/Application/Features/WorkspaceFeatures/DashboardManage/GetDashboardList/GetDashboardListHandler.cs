using System;
using Application.Common.Results;
using Application.Contract.DashboardDtos;
using Application.Helper;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Support.Widget;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.DashboardManage.GetDashboardList;

public class GetDashboardListHandler : IRequestHandler<GetDashboardListQuery, PagedResult<DashboardListItemDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly CursorHelper _cursorHelper;
    private Guid CurrentUserId => _currentUserService.CurrentUserId();
    public GetDashboardListHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, CursorHelper cursorHelper)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _cursorHelper = cursorHelper;
    }

    public async Task<PagedResult<DashboardListItemDto>> Handle(GetDashboardListQuery request, CancellationToken cancellationToken)
    {
        var workspace = await _unitOfWork.Set<ProjectWorkspace>()
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.workspaceId, cancellationToken)
            ?? throw new KeyNotFoundException("cant find workspace");

        var pageSize = request.pagination.PageSize;
        var dashboards = await _unitOfWork.Set<Dashboard>()
            .ApplyFilter(request.filter, CurrentUserId)
            .ApplyCursor(request.pagination, _cursorHelper)
            .ApplySort(request.pagination)
            .Take(pageSize + 1)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var hasMore = dashboards.Count > pageSize;
        var displayItems = dashboards.Take(pageSize).ToList();

        var dtos = displayItems.Adapt<List<DashboardListItemDto>>();
        string? nextCursor = null;
        if (hasMore && displayItems.Count > 0)
        {
            var lastItem = displayItems.Last();

            // NOTE: Using UpdatedAt and Id for the keyset cursor
            nextCursor = _cursorHelper.EncodeCursor(new CursorData(new Dictionary<string, object>
            {
                { "Timestamp", lastItem.UpdatedAt }, // Assuming UpdatedAt is DateTimeOffset
                { "Id", lastItem.Id } // Secondary sort key for uniqueness
            }));
        }

        // 4. Return the result
        return new PagedResult<DashboardListItemDto>(dtos, nextCursor, hasMore);



    }
}
