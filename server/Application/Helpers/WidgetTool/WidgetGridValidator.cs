using System;

using Domain.Entities.Support.Widget;

namespace Application.Helpers.WidgetTool;

public class WidgetGridValidator
{
    private const int GridWidth = 12;
    private const int MinWidgetWidth = 2;
    private const int MinWidgetHeight = 3;

    public GridValidationResult ValidateUpdates(Dashboard dashboard, List<WidgetGridUpdateItem> updates)
    {
        var errors = new List<string>();

        // Check each update
        foreach (var update in updates)
        {
            // 1. Widget exists in dashboard
            if (!dashboard.Widgets.Any(w => w.Id == update.DashboardWidgetId))
            {
                errors.Add($"Widget {update.DashboardWidgetId} not in dashboard");
                continue;
            }

            // 2. Size constraints
            if (update.NewWidth < MinWidgetWidth)
                errors.Add($"Widget width must be >= {MinWidgetWidth}");

            if (update.NewHeight < MinWidgetHeight)
                errors.Add($"Widget height must be >= {MinWidgetHeight}");

            // 3. Grid bounds
            if (update.NewCol < 0 || update.NewRow < 0)
                errors.Add("Widget position cannot be negative");

            if (update.NewCol + update.NewWidth > GridWidth)
                errors.Add($"Widget exceeds grid width of {GridWidth}");
        }

        // 4. No overlaps (frontend should've handled, but validate)
        var updatedWidgets = BuildUpdatedState(dashboard, updates);
        var overlaps = DetectOverlaps(updatedWidgets);
        if (overlaps.Any())
            errors.Add($"Overlapping widgets detected: {string.Join(", ", overlaps)}");

        return new GridValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    private List<GridWidget> BuildUpdatedState(Dashboard dashboard, List<WidgetGridUpdateItem> updates)
    {
        var result = new List<GridWidget>();

        foreach (var widget in dashboard.Widgets)
        {
            var update = updates.FirstOrDefault(u => u.DashboardWidgetId == widget.Id);
            if (update != null)
            {
                result.Add(new GridWidget
                {
                    Id = widget.Id,
                    Col = update.NewCol,
                    Row = update.NewRow,
                    Width = update.NewWidth,
                    Height = update.NewHeight
                });
            }
            else
            {
                result.Add(new GridWidget
                {
                    Id = widget.Id,
                    Col = widget.Layout.Col,
                    Row = widget.Layout.Row,
                    Width = widget.Layout.Width,
                    Height = widget.Layout.Height
                });
            }
        }

        return result;
    }

    private List<string> DetectOverlaps(List<GridWidget> widgets)
    {
        var overlaps = new List<string>();

        for (int i = 0; i < widgets.Count; i++)
        {
            for (int j = i + 1; j < widgets.Count; j++)
            {
                if (CheckCollision(widgets[i], widgets[j]))
                    overlaps.Add($"{widgets[i].Id} overlaps {widgets[j].Id}");
            }
        }

        return overlaps;
    }

    private bool CheckCollision(GridWidget a, GridWidget b)
    {
        return a.Col < b.Col + b.Width &&
               a.Col + a.Width > b.Col &&
               a.Row < b.Row + b.Height &&
               a.Row + a.Height > b.Row;
    }

    public class GridWidget
    {
        public Guid Id { get; set; }
        public int Col { get; set; }
        public int Row { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
public class GridValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class WidgetGridUpdateItem
{
    public Guid DashboardWidgetId { get; set; }
    public int NewCol { get; set; }
    public int NewRow { get; set; }
    public int NewWidth { get; set; }
    public int NewHeight { get; set; }
}