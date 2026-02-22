using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Common;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Domain.Enums.Widget;

namespace Domain.Entities.ProjectEntities;

public class Dashboard : Entity
{
    private const int MaxGridCols = 12;
    private const int MaxGridRows = 2000;
    private const int MaxCascadeDepth = 1000; // Safety limit for cascade chains

    public EntityLayerType LayerType { get; private set; }
    public Guid LayerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsShared { get; private set; }
    public bool IsMain { get; private set; } = false;

    private readonly List<Widget> _widgets = new();
    public IReadOnlyCollection<Widget> Widgets => _widgets.AsReadOnly();

    private GridOccupancyTracker _occupancyTracker = new(MaxGridCols, MaxGridRows);

    private Dashboard() { }

    private Dashboard(Guid id, EntityLayerType layerType, Guid layerId, string name, bool isShared, Guid creatorId, bool isMain = false)
        : base(id)
    {
        LayerType = layerType;
        LayerId = layerId;
        Name = name;
        IsShared = isShared;
        IsMain = isMain;
        CreatorId = creatorId;
    }

    public static Dashboard CreateWorkspaceDashboard(Guid workspaceId, Guid creatorId, string name, bool isShared = false, bool isMain = false)
    {
        if (creatorId == Guid.Empty) throw new ArgumentException("CreatorId cannot be empty.", nameof(creatorId));
        return new(Guid.NewGuid(), EntityLayerType.ProjectWorkspace, workspaceId, name, isShared, creatorId, isMain);
    }

    public static Dashboard CreateScopedDashboard(EntityLayerType layerType, Guid layerId, Guid creatorId, string name, bool isShared = false, bool isMain = false)
    {
        if (layerType == EntityLayerType.ProjectWorkspace) throw new ArgumentException("Use CreateWorkspaceDashboard for workspace scope.", nameof(layerType));
        if (layerId == Guid.Empty) throw new ArgumentException("LayerId cannot be empty.", nameof(layerId));
        if (creatorId == Guid.Empty) throw new ArgumentException("CreatorId cannot be empty.", nameof(creatorId));
        return new(Guid.NewGuid(), layerType, layerId, name, isShared, creatorId, isMain);
    }

    public void RebuildOccupancyTracker()
    {
        _occupancyTracker = new GridOccupancyTracker(MaxGridCols, MaxGridRows);
        foreach (var widget in _widgets)
        {
            _occupancyTracker.MarkOccupied(widget.Layout.Col, widget.Layout.Row, widget.Layout.Width, widget.Layout.Height);
        }
    }

    public void AddWidget(WidgetType widgetType, string configJson, WidgetVisibility visibility, int width, int height, Guid creatorId)
    {
        ValidateWidgetDimensions(width, height);

        var newLayout = FindNextAvailablePosition(width, height);
        var widget = new Widget(Guid.NewGuid(), Id, newLayout, LayerType, LayerId,widgetType, configJson, visibility, creatorId);

        _widgets.Add(widget);
        _occupancyTracker.MarkOccupied(newLayout.Col, newLayout.Row, width, height);
        UpdateTimestamp();
    }

    public void RemoveWidget(Guid widgetId)
    {
        var widget = _widgets.FirstOrDefault(w => w.Id == widgetId);
        if (widget != null)
        {
            _occupancyTracker.UnmarkOccupied(widget.Layout.Col, widget.Layout.Row, widget.Layout.Width, widget.Layout.Height);
            _widgets.Remove(widget);
            UpdateTimestamp();
        }
    }

    public void MoveWidget(Guid widgetId, int newCol, int newRow)
    {
        var widget = _widgets.FirstOrDefault(w => w.Id == widgetId);
        if (widget == null) return;

        ValidatePosition(newCol, newRow);

        var collidingWidgets = FindCollidingWidgetsInColumn(newCol, newRow, widget.Layout.Width, widget.Layout.Height, widgetId);

        _occupancyTracker.UnmarkOccupied(widget.Layout.Col, widget.Layout.Row, widget.Layout.Width, widget.Layout.Height);

        if (collidingWidgets.Any())
        {
            CascadeWidgetsDown(collidingWidgets, widget.Layout.Height);
        }

        widget.UpdateLayout(widget.Layout.WithPosition(newCol, newRow));
        _occupancyTracker.MarkOccupied(newCol, newRow, widget.Layout.Width, widget.Layout.Height);

        UpdateTimestamp();
    }

    public void ResizeWidget(Guid widgetId, int newWidth, int newHeight)
    {
        var widget = _widgets.FirstOrDefault(w => w.Id == widgetId);
        if (widget == null) return;

        ValidateWidgetDimensions(newWidth, newHeight);

        int oldHeight = widget.Layout.Height;
        int heightDifference = newHeight - oldHeight;

        if (heightDifference > 0)
        {
            int checkStartRow = widget.Layout.Row + oldHeight;
            var collidingWidgets = FindCollidingWidgetsInColumn(widget.Layout.Col, checkStartRow, newWidth, heightDifference, widgetId);

            if (collidingWidgets.Any())
            {
                CascadeWidgetsDown(collidingWidgets, heightDifference);
            }
        }

        _occupancyTracker.UnmarkOccupied(widget.Layout.Col, widget.Layout.Row, widget.Layout.Width, widget.Layout.Height);

        var newLayout = new WidgetLayout(widget.Layout.Col, widget.Layout.Row, newWidth, newHeight);
        widget.UpdateLayout(newLayout);
        _occupancyTracker.MarkOccupied(widget.Layout.Col, widget.Layout.Row, newWidth, newHeight);

        UpdateTimestamp();
    }

    public void UpdateMain(bool isMain)
    {
        IsMain = isMain;
        UpdateTimestamp();
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty");
        Name = name;
        UpdateTimestamp();
    }

    public void UpdateShared(bool isShared)
    {
        IsShared = isShared;
        UpdateTimestamp();
    }

    private List<Widget> FindCollidingWidgetsInColumn(int col, int row, int width, int height, Guid excludeWidgetId)
    {
        var colliding = new List<Widget>();

        foreach (var widget in _widgets.Where(w => w.Id != excludeWidgetId && w.Layout.Col == col))
        {
            if (widget.Layout.Row < row + height && widget.Layout.Row + widget.Layout.Height > row)
            {
                colliding.Add(widget);
            }
        }

        return colliding.OrderBy(w => w.Layout.Row).ToList();
    }

    private void CascadeWidgetsDown(List<Widget> widgetsToMove, int pushDistance)
    {
        if (widgetsToMove.Count == 0) return;
        if (widgetsToMove.Count > MaxCascadeDepth)
            throw new InvalidOperationException($"Cascade depth exceeds limit ({MaxCascadeDepth}). Too many widgets would shift.");

        foreach (var widget in widgetsToMove)
        {
            _occupancyTracker.UnmarkOccupied(widget.Layout.Col, widget.Layout.Row, widget.Layout.Width, widget.Layout.Height);

            int newRow = widget.Layout.Row + pushDistance;

            if (newRow < 0 || newRow >= MaxGridRows)
                throw new InvalidOperationException($"Cascade would push widget beyond canvas bounds (max rows: {MaxGridRows})");

            widget.UpdateLayout(widget.Layout.WithPosition(widget.Layout.Col, newRow));
            _occupancyTracker.MarkOccupied(widget.Layout.Col, newRow, widget.Layout.Width, widget.Layout.Height);
        }
    }

    private WidgetLayout FindNextAvailablePosition(int widgetWidth, int widgetHeight)
    {
        const int maxScanRows = 1000;
        int scanLimit = Math.Min(_occupancyTracker.GetMaxScanRow(widgetHeight) + 1, maxScanRows);

        for (int row = 0; row < scanLimit; row++)
        {
            for (int col = 0; col <= MaxGridCols - widgetWidth; col++)
            {
                if (_occupancyTracker.CanPlaceAt(col, row, widgetWidth, widgetHeight))
                {
                    return new WidgetLayout(col, row, widgetWidth, widgetHeight);
                }
            }
        }

        throw new InvalidOperationException("Dashboard grid is full, cannot place widget");
    }

    private void ValidateWidgetDimensions(int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Widget dimensions must be positive");
        if (width > MaxGridCols)
            throw new ArgumentException($"Widget width cannot exceed {MaxGridCols}");
        if (height > MaxGridRows)
            throw new ArgumentException($"Widget height cannot exceed {MaxGridRows}");
    }

    private void ValidatePosition(int col, int row)
    {
        if (col < 0 || row < 0)
            throw new ArgumentException("Position cannot be negative");
        if (col >= MaxGridCols || row >= MaxGridRows)
            throw new ArgumentException("Position exceeds canvas bounds");
    }
}
