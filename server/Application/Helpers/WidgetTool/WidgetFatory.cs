using System.Text.Json;
using Domain.Enums.Widget;

namespace Application.Helpers.WidgetTool;

public static class WidgetFactory
{
    public static string CreateWidgetConfig(WidgetType type, WidgetFilter? filter = null)
    {
        return type switch
        {
            WidgetType.TaskList => CreateTaskListWidgetConfig(filter),
            _ => throw new InvalidOperationException($"Unknown widget type: {type}")
        };
    }

    public static (int Width, int Height) GetDefaultWidgetDimensions(WidgetType type)
    {
        return type switch
        {
            WidgetType.TaskList => (4, 3), // Example: TaskList widget is 4 units wide, 3 units high
            // Add more widget types and their default dimensions here
            _ => (2, 2) // Default for unknown types
        };
    }

    private static string CreateTaskListWidgetConfig(WidgetFilter? filter)
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
        return JsonSerializer.Serialize(taskListConfig);
    }
}
