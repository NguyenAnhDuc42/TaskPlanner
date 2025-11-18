using System;
using Domain.Common;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Domain.Enums.Widget;

namespace Domain.Entities.Support.Widget;

public class Dashboard : Entity
{
    public EntityLayerType LayerType { get; private set; }
    public Guid LayerId { get; private set; }
    public Guid CreatorId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsShared { get; private set; }
    public bool IsMain { get; private set; } = false;

    private readonly List<Widget> _widgets = new();
    public IReadOnlyCollection<Widget> Widgets => _widgets.AsReadOnly();

    private Dashboard() { } // EF

    private Dashboard(Guid id, EntityLayerType layerType, Guid layerId, Guid creatorId, string name, bool isShared, bool isMain = false)
        : base(id)
    {
        LayerType = layerType;
        LayerId = layerId;
        CreatorId = creatorId;
        Name = name;
        IsShared = isShared;
        IsMain = isMain;
    }

    public static Dashboard CreateWorkspaceDashboard(Guid workspaceId, Guid creatorId, string name, bool isShared = false, bool isMain = false)
    {
        if (creatorId == Guid.Empty) throw new ArgumentException("CreatorId cannot be empty.", nameof(creatorId));
        return new(Guid.NewGuid(), EntityLayerType.ProjectWorkspace, workspaceId, creatorId, name, isShared, isMain);
    }

    public static Dashboard CreateScopedDashboard(EntityLayerType layerType, Guid layerId, Guid creatorId, string name, bool isShared = false, bool isMain = false)
    {
        if (layerType == EntityLayerType.ProjectWorkspace) throw new ArgumentException("Use CreateWorkspaceDashboard for workspace scope.", nameof(layerType));
        if (layerId == Guid.Empty) throw new ArgumentException("LayerId cannot be empty.", nameof(layerId));
        if (creatorId == Guid.Empty) throw new ArgumentException("CreatorId cannot be empty.", nameof(creatorId));
        return new(Guid.NewGuid(), layerType, layerId, creatorId, name, isShared, isMain);
    }

    public void AddWidget(WidgetType widgetType, string configJson, WidgetVisibility visibility, int width, int height, int order = 0)
    {
        if (CreatorId == Guid.Empty) throw new ArgumentException("CreatorId cannot be empty.", nameof(CreatorId));

        // Find the next available position using the provided dimensions
        var newWidgetLayout = FindNextAvailablePosition(width, height);

        // 3. Create the widget with the determined layout
        var widget = new Widget(Guid.NewGuid(), Id, order, newWidgetLayout, LayerType, LayerId, CreatorId, widgetType, configJson, visibility);
        _widgets.Add(widget);
        UpdateTimestamp();
    }

    public void RemoveWidget(Guid widgetId)
    {
        var removed = _widgets.RemoveAll(w => w.Id == widgetId) > 0;
        if (removed) UpdateTimestamp();
    }

    public void MoveWidget(Guid widgetId, int newCol, int newRow)
    {
        var target = _widgets.FirstOrDefault(w => w.Id == widgetId);
        if (target == null) return;
        var updated = target.Layout.WithPosition(newCol, newRow);
        target.UpdateLayout(updated);
        UpdateTimestamp();
    }

    public void UpdateWidgetPosition(Guid widgetId, int newCol, int newRow, int newWidth, int newHeight)
    {
        var widget = _widgets.FirstOrDefault(w => w.Id == widgetId);
        if (widget == null) return;

        var newLayout = new WidgetLayout(newCol, newRow, newWidth, newHeight);
        widget.UpdateLayout(newLayout);
        UpdateTimestamp();
    }
    public void UpdateMain(bool isMain)
    {
        IsMain = isMain;
        UpdateTimestamp();
    }
    private WidgetLayout FindNextAvailablePosition(int widgetWidth, int widgetHeight, int maxGridCols = 12)
    {
        for (int row = 0; ; row++)
        {
            for (int col = 0; col <= maxGridCols - widgetWidth; col++)
            {
                bool overlaps = false;
                foreach (var existingWidget in _widgets)
                {
                    if (CheckForOverlap(existingWidget.Layout, col, row, widgetWidth, widgetHeight))
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps)
                {
                    return new WidgetLayout(col, row, widgetWidth, widgetHeight);
                }
            }
        }
    }

    private bool CheckForOverlap(WidgetLayout existingLayout, int newCol, int newRow, int newWidth, int newHeight)
    {
        if (newCol >= existingLayout.Col + existingLayout.Width || existingLayout.Col >= newCol + newWidth)
            return false;

        if (newRow >= existingLayout.Row + existingLayout.Height || existingLayout.Row >= newRow + newHeight)
            return false;

        return true;
    }
}