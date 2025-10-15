using System;
using Domain.Common;
using Domain.Enums;
using Domain.Enums.Widget;

namespace Domain.Entities.Support.Widget;

public class Widget : Entity
{
    public ScopeType ScopeType { get; init; }
    public Guid ScopeId { get; init; }
    public Guid CreatorId { get; init; }
    public WidgetVisibility Visibility { get; private set; }
    public WidgetType WidgetType { get; init; }
    public string ConfigJson { get; private set; } = "{}";

    private Widget() { } // EF

    private Widget(Guid id, ScopeType scopeType, Guid scopeId, Guid ownerId, WidgetType widgetType, string configJson, WidgetVisibility visibility)
        : base(id)
    {
        ScopeType = scopeType;
        ScopeId = scopeId;
        CreatorId = ownerId;
        WidgetType = widgetType;
        ConfigJson = configJson ?? "{}";
        Visibility = visibility;
    }

    public static Widget Create(ScopeType scopeType, Guid scopeId, Guid ownerId, WidgetType widgetType, string configJson, WidgetVisibility visibility = WidgetVisibility.Private)
        => new(Guid.NewGuid(), scopeType, scopeId, ownerId, widgetType, configJson, visibility);

    public void SetVisibility(WidgetVisibility visibility)
    {
        Visibility = visibility;
        UpdateTimestamp();
    }

    public void UpdateConfig(string configJson)
    {
        ConfigJson = configJson ?? "{}";
        UpdateTimestamp();
    }
}
