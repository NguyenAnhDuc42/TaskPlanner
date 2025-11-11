using System;
using Domain.Common;
using Domain.Enums;
using Domain.Enums.Widget;

namespace Domain.Entities.Support.Widget;

public class Widget : Entity
{
    public ScopeType ScopeType { get; private set; }
    public Guid ScopeId { get; private set; }
    public Guid CreatorId { get; private set; }
    public WidgetType WidgetType { get; private set; }
    public WidgetVisibility Visibility { get; private set; }
    public string ConfigJson { get; private set; } = "{}";

    private Widget() { } // EF

    private Widget(Guid id, ScopeType scopeType, Guid scopeId, Guid creatorId, WidgetType widgetType, string configJson, WidgetVisibility visibility)
        : base(id)
    {
        ScopeType = scopeType;
        ScopeId = scopeId;
        CreatorId = creatorId;
        WidgetType = widgetType;
        ConfigJson = string.IsNullOrWhiteSpace(configJson) ? "{}" : configJson;
        Visibility = visibility;
    }

    public static Widget Create(ScopeType scopeType, Guid scopeId, Guid creatorId, WidgetType widgetType, string configJson, WidgetVisibility visibility = WidgetVisibility.Private)
    {
        if (creatorId == Guid.Empty) throw new ArgumentException("CreatorId cannot be empty.", nameof(creatorId));
        return new(Guid.NewGuid(), scopeType, scopeId, creatorId, widgetType, configJson, visibility);
    }

    public void SetVisibility(WidgetVisibility visibility)
    {
        Visibility = visibility;
        UpdateTimestamp();
    }

    public void UpdateConfig(string configJson)
    {
        ConfigJson = string.IsNullOrWhiteSpace(configJson) ? "{}" : configJson;
        UpdateTimestamp();
    }
}
