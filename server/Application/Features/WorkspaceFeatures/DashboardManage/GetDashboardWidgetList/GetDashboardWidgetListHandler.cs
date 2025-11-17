using System;
using System.Text.Json;
using Application.Contract.WidgetDtos;
using Application.Helpers.WidgetTool;
using Application.Helpers.WidgetTool.WidgetQueryBuilder;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Support.Widget;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.WorkspaceFeatures.DashboardManage.GetDashboardWidgetList;

public class GetDashboardWidgetListHandler : IRequestHandler<GetDashboardWidgetListQuery, DashboardWidgetListDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly WidgetBuilder _widgetBuilder;
    public GetDashboardWidgetListHandler(IUnitOfWork unitOfWork, WidgetBuilder widgetBuilder)
    {
        _unitOfWork = unitOfWork;
        _widgetBuilder = widgetBuilder;
    }
    public async Task<DashboardWidgetListDto> Handle(GetDashboardWidgetListQuery request, CancellationToken cancellationToken)
    {

        var workspace = await _unitOfWork.Set<ProjectWorkspace>()
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.workspaceId, cancellationToken)
            ?? throw new KeyNotFoundException("cant find workspace");

        var dashboard = request.dashboardId != null
        ? await _unitOfWork.Set<Dashboard>()
            .AsNoTracking()
            .Include(d => d.Widgets)
            .FirstOrDefaultAsync(d => d.Id == request.dashboardId, cancellationToken)

        : await _unitOfWork.Set<Dashboard>()
            .AsNoTracking()
            .Include(d => d.Widgets)
            .FirstOrDefaultAsync(d => d.LayerId == request.workspaceId && d.LayerType == EntityLayerType.ProjectWorkspace && d.IsMain == true, cancellationToken);

        if (dashboard == null) throw new KeyNotFoundException("cant find dashboard");

        var widgetIds = dashboard.Widgets.Select(dw => dw.WidgetId).ToList();
        if (!widgetIds.Any())
            return new DashboardWidgetListDto
            {
                DashboardId = dashboard.Id,
                Name = dashboard.Name,
                LayerType = dashboard.LayerType,
                Widgets = new()
            };
        var widgets = await _unitOfWork.Set<Widget>()
            .AsNoTracking()
            .Where(w => widgetIds.Contains(w.Id))
            .ToListAsync(cancellationToken);

        var widgetDataList = new List<WidgetDto>();

        foreach (var dashboardWidget in dashboard.Widgets)
        {
            var widget = widgets.FirstOrDefault(w => w.Id == dashboardWidget.WidgetId);
            if (widget == null) continue;

            var filter = JsonSerializer.Deserialize<WidgetFilter>(widget.ConfigJson)
                ?? throw new InvalidOperationException("Failed to deserialize widget config");

            var widgetData = await _widgetBuilder.ExecuteAsync(widget.WidgetType, widget.LayerId, widget.LayerType, filter, cancellationToken);

            widgetDataList.Add(new WidgetDto
            {
                WidgetId = widget.Id,
                Type = widget.WidgetType,
                ConfigJson = widget.ConfigJson,
                Data = widgetData,
                Layout = new WidgetLayoutDto
                {
                    Col = dashboardWidget.Layout.Col,
                    Row = dashboardWidget.Layout.Row,
                    Width = dashboardWidget.Layout.Width,
                    Height = dashboardWidget.Layout.Height
                }
            });
        }
        return new DashboardWidgetListDto
        {
            DashboardId = dashboard.Id,
            Name = dashboard.Name,
            LayerType = dashboard.LayerType,
            Widgets = widgetDataList
        };

    }
}
