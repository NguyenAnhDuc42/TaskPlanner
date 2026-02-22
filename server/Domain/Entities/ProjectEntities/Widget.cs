using System;
using Domain.Common;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Domain.Enums.Widget;

namespace Domain.Entities.ProjectEntities;

public class Widget : Entity
{
    public Guid DashboardId { get; private set; }
    public WidgetLayout Layout { get; private set; } = new WidgetLayout(0, 0, 2, 2);
    public EntityLayerType LayerType { get; private set; }
    public Guid LayerId { get; private set; }
    public WidgetType WidgetType { get; private set; }
    public WidgetVisibility Visibility { get; private set; }
    public string ConfigJson { get; private set; } = "{}";

    private Widget() { } // EF

    internal Widget(Guid id, Guid dashboardId, WidgetLayout layout, EntityLayerType layerType, Guid layerId, WidgetType widgetType, string configJson, WidgetVisibility visibility, Guid creatorId)
        : base(id)
    {
        DashboardId = dashboardId;
        Layout = layout;
        LayerType = layerType;
        LayerId = layerId;
        WidgetType = widgetType;
        ConfigJson = string.IsNullOrWhiteSpace(configJson) ? "{}" : configJson;
        Visibility = visibility;
        CreatorId = creatorId;
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

    public void UpdateLayout(WidgetLayout layout)
    {
        Layout = layout;
        UpdateTimestamp();
    }
}
