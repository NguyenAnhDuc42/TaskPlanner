using System;
using System.Text.Json;
using Domain.Entities.Support.Widget;
using Domain.Enums.RelationShip;
using Domain.Enums.Widget;

namespace Application.Helpers.WidgetTool;

public class WidgetFatory
{
    public Widget CreateWidget(WidgetType type, EntityLayerType layerType, Guid layerId, Guid userId, WidgetFilter? filter = null)
    {
        return type switch
        {
            WidgetType.TaskList => CreateTaskListWidget(layerType, layerId, userId, filter),

            _ => throw new InvalidOperationException($"Unknown widget type: {type}")
        };
    }

    private Widget CreateTaskListWidget(EntityLayerType layerType, Guid layerId, Guid userId, WidgetFilter? filter)
    {
        var defaultFilter = new WidgetFilter { Limit = 10, StatusIds = new() };
        var mergedFilter = filter ?? defaultFilter;
        var taskListConfig = new WidgetFilter
        {
            SearchText = mergedFilter.SearchText,
            TagIds = mergedFilter.TagIds,
            StatusIds = mergedFilter.StatusIds,
            PriorityIds = mergedFilter.PriorityIds,
            DateFrom = mergedFilter.DateFrom,
            DateTo = mergedFilter.DateTo,
            Limit = mergedFilter.Limit
        };
        var configJson = JsonSerializer.Serialize(taskListConfig);

        return Widget.Create(
            layerType: layerType,  // Use passed layer type
            layerId: layerId,
            creatorId: userId,
            widgetType: WidgetType.TaskList,
            configJson: configJson
        );
    }
}
