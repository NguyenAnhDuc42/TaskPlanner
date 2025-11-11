using System;
using Domain.Common;
using Domain.Enums;
using Domain.Enums.Widget;

namespace Domain.Entities.Support.Widget;

public class Dashboard : Entity
{
    public ScopeType Scope { get; private set; }
    public Guid ScopeId { get; private set; }
    public Guid CreatorId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsShared { get; private set; }

    private readonly List<DashboardWidget> _widgets = new();
    public IReadOnlyCollection<DashboardWidget> Widgets => _widgets.AsReadOnly();

    private Dashboard() { } // EF

    private Dashboard(Guid id, ScopeType scope, Guid scopeId, Guid creatorId, string name, bool isShared)
        : base(id)
    {
        Scope = scope;
        ScopeId = scopeId;
        CreatorId = creatorId;
        Name = name;
        IsShared = isShared;
    }

    public static Dashboard CreateWorkspaceDashboard(Guid id, Guid workspaceId, Guid creatorId, string name)
    {
        if (workspaceId == Guid.Empty) throw new ArgumentException("WorkspaceId cannot be empty.", nameof(workspaceId));
        if (creatorId == Guid.Empty) throw new ArgumentException("CreatorId cannot be empty.", nameof(creatorId));
        return new(id, ScopeType.Workspace, workspaceId, creatorId, name, isShared: true);
    }

    public static Dashboard CreateScopedDashboard(Guid id, ScopeType scope, Guid scopeId, Guid creatorId, string name)
    {
        if (scope == ScopeType.Workspace) throw new ArgumentException("Use CreateWorkspaceDashboard for workspace scope.", nameof(scope));
        if (scopeId == Guid.Empty) throw new ArgumentException("ScopeId cannot be empty.", nameof(scopeId));
        if (creatorId == Guid.Empty) throw new ArgumentException("CreatorId cannot be empty.", nameof(creatorId));
        return new(id, scope, scopeId, creatorId, name, isShared: false);
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

    public void Reorder(Guid dashboardWidgetId, int newOrder)
    {
        var target = _widgets.FirstOrDefault(w => w.Id == dashboardWidgetId);
        if (target == null) return;
        target.SetOrder(newOrder);
        _widgets.Sort((a, b) => a.Order.CompareTo(b.Order));
        UpdateTimestamp();
    }

    public void MoveWidget(Guid dashboardWidgetId, int newCol, int newRow)
    {
        var target = _widgets.FirstOrDefault(w => w.Id == dashboardWidgetId);
        if (target == null) return;
        var updated = target.Layout.WithPosition(newCol, newRow);
        target.UpdateLayout(updated);
        UpdateTimestamp();
    }

    public void ResizeWidget(Guid dashboardWidgetId, int newWidth, int newHeight)
    {
        var target = _widgets.FirstOrDefault(w => w.Id == dashboardWidgetId);
        if (target == null) return;
        var updated = target.Layout.WithSize(newWidth, newHeight);
        target.UpdateLayout(updated);
        UpdateTimestamp();
    }
}