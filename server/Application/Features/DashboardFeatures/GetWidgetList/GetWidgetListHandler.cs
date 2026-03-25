
using Application.Common.Results;
using Application.Features.DashboardFeatures.WidgetDataHelper;
using Application.Helper;
using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.DashboardFeatures.GetWidgetList;

public class GetWidgetListHandler : BaseFeatureHandler, IRequestHandler<GetWidgetListQuery, PagedResult<WidgetDto>>
{
    private readonly CursorHelper _cursorHelper;
    private readonly WidgetBuilder _widgetBuilder;

    public GetWidgetListHandler(
        IUnitOfWork unitOfWork, 
        ICurrentUserService currentUserService, 
        WorkspaceContext workspaceContext,
        CursorHelper cursorHelper,
        WidgetBuilder widgetBuilder)
        : base(unitOfWork, currentUserService, workspaceContext) 
    {
        _cursorHelper = cursorHelper;
        _widgetBuilder = widgetBuilder;
    }

    public async Task<PagedResult<WidgetDto>> Handle(GetWidgetListQuery request, CancellationToken cancellationToken)
    {
        var pageSize = request.pagination.PageSize;

        // 1. Metadata Query (Fast)
        var query = UnitOfWork.Set<Widget>()
            .AsNoTracking()
            .ApplyFilter(request.dashboardId)
            .ApplyCursor(request.pagination, _cursorHelper)
            .ApplySort(request.pagination);

        var widgets = await query
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = widgets.Count > pageSize;
        if (hasMore) widgets.RemoveAt(widgets.Count - 1);

        var dtos = widgets.Select(w => new WidgetDto(
            w.Id,
            w.DashboardId,
            new WidgetLayoutDto(w.Layout.Col, w.Layout.Row, w.Layout.Width, w.Layout.Height),
            w.WidgetType,
            w.ConfigJson,
            w.UpdatedAt)).ToList();

        // 2. Async Data Building (Fire-and-forget)
        if (widgets.Any())
        {
            _ = _widgetBuilder.BuildAndNotifyAsync(widgets, CurrentUserId, cancellationToken);
        }

        // 3. Return Metadata Immediately
        var nextCursor = hasMore && widgets.Count > 0
            ? EncodeCursor(widgets.Last())
            : null;

        return new PagedResult<WidgetDto>(dtos, nextCursor, hasMore);
    }

    private string EncodeCursor(Widget last)
    {
        return _cursorHelper.EncodeCursor(new CursorData(new Dictionary<string, object>
        {
            { "Timestamp", last.UpdatedAt },
            { "Id", last.Id }
        }));
    }
}
