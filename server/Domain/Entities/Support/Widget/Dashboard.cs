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

    private readonly List<DashboardWidget> _widgets = new();
    public IReadOnlyCollection<DashboardWidget> Widgets => _widgets.AsReadOnly();

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

    public void AddWidget(Guid widgetId, int order = 0, WidgetLayout? layout = null)
    {
        if (widgetId == Guid.Empty) throw new ArgumentException("WidgetId cannot be empty.", nameof(widgetId));
        var dw = new DashboardWidget(Id, widgetId, order, layout);
        _widgets.Add(dw);
        UpdateTimestamp();
    }

    public void RemoveWidget(Guid dashboardWidgetId)
    {
        var removed = _widgets.RemoveAll(w => w.Id == dashboardWidgetId) > 0;
        if (removed) UpdateTimestamp();
    }

    public void MoveWidget(Guid dashboardWidgetId, int newCol, int newRow)
    {
        var target = _widgets.FirstOrDefault(w => w.Id == dashboardWidgetId);
        if (target == null) return;
        var updated = target.Layout.WithPosition(newCol, newRow);
        target.UpdateLayout(updated);
        UpdateTimestamp();
    }

    public void UpdateWidgetPosition(Guid dashboardWidgetId, int newCol, int newRow, int newWidth, int newHeight)
    {
        var widget = _widgets.FirstOrDefault(w => w.Id == dashboardWidgetId);
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
}