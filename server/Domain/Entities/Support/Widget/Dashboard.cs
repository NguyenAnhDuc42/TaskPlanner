using System;
using Domain.Common;
using Domain.Enums;
using Domain.Enums.Widget;

namespace Domain.Entities.Support.Widget;

public class Dashboard : Entity
{
    public Guid OwnerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ScopeType ScopeType { get; private set; }
    public Guid ScopeId { get; private set; }
    public WidgetVisibility Visibility { get; private set; }

    // dashboard's canonical widget references + order
    private readonly List<DashboardWidget> _widgets = new();
    public IReadOnlyCollection<DashboardWidget> Widgets => _widgets.AsReadOnly();

    private Dashboard() { } // EF

    public Dashboard(Guid id, Guid ownerId, string name, ScopeType scopeType, Guid scopeId, WidgetVisibility visibility)
        : base(id)
    {
        OwnerId = ownerId;
        Name = name;
        ScopeType = scopeType;
        ScopeId = scopeId;
        Visibility = visibility;
    }

    public void AddWidget(Guid widgetDefinitionId, string? defaultConfigJson = null, int? order = null)
    {
        var dw = new DashboardWidget(Id, widgetDefinitionId, order ?? _widgets.Count, defaultConfigJson);
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
}
