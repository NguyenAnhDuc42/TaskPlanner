using System;
using System.Text.Json;
using Application.Contract.WidgetDtos;
using Application.Helpers.WidgetTool;
using Application.Helpers.WidgetTool.WidgetQueryBuilder;
using Application.Interfaces.Repositories;
using Domain.Entities.Support.Widget;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.DashboardManagement.GetWidgetList;

public class GetWidgetListHandler : IRequestHandler<GetWidgetListQuery, DashboardWidgetListDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly WidgetBuilder _widgetBuilder;

    public GetWidgetListHandler(IUnitOfWork unitOfWork, WidgetBuilder widgetBuilder)
    {
        _unitOfWork = unitOfWork;
        _widgetBuilder = widgetBuilder;
    }

    public async Task<DashboardWidgetListDto> Handle(GetWidgetListQuery request, CancellationToken cancellationToken)
    {
        // Get dashboard by ID with widgets loaded
        var dashboard = await _unitOfWork.Set<Dashboard>()
            .AsNoTracking()
            .Include(d => d.Widgets)
            .FirstOrDefaultAsync(d => d.Id == request.dashboardId, cancellationToken)
            ?? throw new KeyNotFoundException("Dashboard not found");

        // Early exit if no widgets
        if (!dashboard.Widgets.Any())
        {
            return new DashboardWidgetListDto
            {
                DashboardId = dashboard.Id,
                Name = dashboard.Name,
                LayerType = dashboard.LayerType,
                Widgets = new()
            };
        }

        // Build widget data list - widgets already loaded via Include
        var widgetDataList = new List<WidgetDto>();

        foreach (var widget in dashboard.Widgets)
        {
            try
            {
                // Deserialize config
                var filter = JsonSerializer.Deserialize<WidgetFilter>(widget.ConfigJson)
                    ?? throw new InvalidOperationException($"Failed to deserialize widget config for widget {widget.Id}");

                // Execute widget query builder (fetches actual data)
                var widgetData = await _widgetBuilder.ExecuteAsync(
                    widget.WidgetType,
                    widget.LayerId,
                    widget.LayerType,
                    filter,
                    cancellationToken);

                // Map to DTO
                widgetDataList.Add(new WidgetDto
                {
                    WidgetId = widget.Id,
                    Type = widget.WidgetType,
                    ConfigJson = widget.ConfigJson,
                    Data = widgetData,
                    Layout = new WidgetLayoutDto
                    {
                        Col = widget.Layout.Col,
                        Row = widget.Layout.Row,
                        Width = widget.Layout.Width,
                        Height = widget.Layout.Height
                    }
                });
            }
            catch (Exception ex)
            {
                // Log but continue - don't fail entire dashboard if one widget fails
                // Log.Error($"Failed to build widget {widget.Id}: {ex.Message}");
                continue;
            }
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

