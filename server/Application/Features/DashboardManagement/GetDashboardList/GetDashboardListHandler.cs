using Application.Common.Results;
using Application.Contract.DashboardDtos;
using Application.Helper;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Application.Helpers;
using Domain.Entities.Support.Widget;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.DashboardManagement.GetDashboardList;

public class GetDashboardListHandler : BaseQueryHandler, IRequestHandler<GetDashboardListQuery, PagedResult<DashboardListItemDto>>
{
    public GetDashboardListHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext, CursorHelper cursorHelper)
    : base(unitOfWork, permissionService, currentUserService, workspaceContext, cursorHelper) { }

    public async Task<PagedResult<DashboardListItemDto>> Handle(GetDashboardListQuery request, CancellationToken cancellationToken)
    {
        var layer = await GetLayer(request.layerId, request.layerType);

        var pageSize = request.pagination.PageSize;
        var dashboards = await UnitOfWork.Set<Dashboard>()
            .ApplyFilter(request.filter, CurrentUserId)
            .ApplyCursor(request.pagination, CursorHelper)
            .ApplySort(request.pagination)
            .Take(pageSize + 1)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var hasMore = dashboards.Count > pageSize;
        var displayItems = dashboards.Take(pageSize).ToList();

        var dtos = displayItems.Select(d => new DashboardListItemDto(d.Id, d.Name, d.UpdatedAt)).ToList();
        string? nextCursor = null;
        if (hasMore && displayItems.Count > 0)
        {
            var lastItem = displayItems.Last();

            // NOTE: Using UpdatedAt and Id for the keyset cursor
            nextCursor = CursorHelper.EncodeCursor(new CursorData(new Dictionary<string, object>
            {
                { "Timestamp", lastItem.UpdatedAt }, // Assuming UpdatedAt is DateTimeOffset
                { "Id", lastItem.Id } // Secondary sort key for uniqueness
            }));
        }

        // 4. Return the result
        return new PagedResult<DashboardListItemDto>(dtos, nextCursor, hasMore);

    }
}
