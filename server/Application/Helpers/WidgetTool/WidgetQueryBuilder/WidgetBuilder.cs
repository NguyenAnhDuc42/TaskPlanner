using System;
using Application.Contract.WidgetDtos;
using Application.Helpers.WidgetTool.WidgetQueryBuilder.WidgetQuery;
using Application.Interfaces.Repositories;
using Domain.Enums.RelationShip;
using Domain.Enums.Widget;

namespace Application.Helpers.WidgetTool.WidgetQueryBuilder;

public class WidgetBuilder
{
    private readonly IUnitOfWork _unitOfWork;
    public WidgetBuilder(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<object> ExecuteAsync(WidgetType widgetType,Guid layerId,EntityLayerType layerType, WidgetFilter filter, CancellationToken cancellationToken)
    {
        return widgetType switch
        {
            _ => throw new InvalidOperationException($"Unknown widget type: {widgetType}")
        };
    }

    private async Task<List<TaskListWidgetItemDto>> ExecuteTaskListAsync(Guid layerId, EntityLayerType layerType, WidgetFilter filter, CancellationToken cancellationToken)
    {
        return await TaskListWidgetQuery.ExecuteAsync(_unitOfWork, layerId, layerType, filter, cancellationToken);
    }
}
