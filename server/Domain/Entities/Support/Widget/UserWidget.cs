using System;
using Domain.Common;

namespace Domain.Entities.Support.Widget;

public class UserWidget : Entity
{
    public Guid DashboardId { get; private set; }   // ties the user widget to a dashboard view
    public Guid UserId { get; private set; }
    public Guid WidgetId { get; private set; } // canonical widget reference
    public int PositionIndex { get; private set; }
    public bool Visible { get; private set; }
    public string? ConfigOverrideJson { get; private set; }
    public WidgetLayout Layout { get; private set; } = new WidgetLayout(0, 0, 2, 2);

    private UserWidget() { } // EF

    private UserWidget(Guid id, Guid dashboardId, Guid userId, Guid widgetId,
                       WidgetLayout layout, int positionIndex, bool visible, string? configOverrideJson)
        : base(id)
    {
        DashboardId = dashboardId;
        UserId = userId;
        WidgetId = widgetId;
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        PositionIndex = positionIndex;
        Visible = visible;
        ConfigOverrideJson = configOverrideJson;
    }

    public static UserWidget CreateForUser(Guid dashboardId, Guid userId, Guid widgetId,
                                           WidgetLayout layout, int position = 0, string? configOverrideJson = null)
        => new(Guid.NewGuid(), dashboardId, userId, widgetId, layout, position, true, configOverrideJson);

    public void Move(int newCol, int newRow)
    {
        var updated = Layout.WithPosition(newCol, newRow);
        if (!updated.Equals(Layout))
        {
            Layout = updated;
            UpdateTimestamp();
        }
    }

    public void Resize(int newWidth, int newHeight)
    {
        var updated = Layout.WithSize(newWidth, newHeight);
        if (!updated.Equals(Layout))
        {
            Layout = updated;
            UpdateTimestamp();
        }
    }

    public void SetVisibility(bool visible)
    {
        if (Visible == visible) return;
        Visible = visible;
        UpdateTimestamp();
    }

    public void UpdateConfigOverride(string? json)
    {
        if (ConfigOverrideJson == json) return;
        ConfigOverrideJson = json;
        UpdateTimestamp();
    }

    public void SetPositionIndex(int index)
    {
        if (PositionIndex == index) return;
        PositionIndex = index;
        UpdateTimestamp();
    }
}