using System;
using Domain.Common;

namespace Domain.Entities.Support.Widget;

public class UserWidget : Entity
{
    public Guid UserId { get; private set; }
    public Guid WidgetId { get; private set; } // canonical widget reference
    public int PositionIndex { get; private set; } // optional linear ordering
    public bool Visible { get; private set; }
    public string? ConfigOverrideJson { get; private set; }

    public WidgetLayout Layout { get; private set; } = new WidgetLayout(0, 0, 2, 2);

    private UserWidget() { } // EF

    private UserWidget(Guid id, Guid userId, Guid widgetId, WidgetLayout layout, int positionIndex, bool visible, string? configOverrideJson)
        : base(id)
    {
        UserId = userId;
        WidgetId = widgetId;
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        PositionIndex = positionIndex;
        Visible = visible;
        ConfigOverrideJson = configOverrideJson;
    }

    public static UserWidget CreateForUser(Guid userId, Guid widgetId, WidgetLayout layout, int position = 0, string? configOverrideJson = null)
        => new(Guid.NewGuid(), userId, widgetId, layout, position, true, configOverrideJson);

    public void Move(int newCol, int newRow)
    {
        var newLayout = Layout.WithPosition(newCol, newRow);
        if (!newLayout.Equals(Layout))
        {
            Layout = newLayout;
            UpdateTimestamp();
        }
    }

    public void Resize(int newWidth, int newHeight)
    {
        var newLayout = Layout.WithSize(newWidth, newHeight);
        if (!newLayout.Equals(Layout))
        {
            Layout = newLayout;
            UpdateTimestamp();
        }
    }

    public void SetVisibility(bool visible)
    {
        if (Visible != visible)
        {
            Visible = visible;
            UpdateTimestamp();
        }
    }

    public void UpdateConfigOverride(string? json)
    {
        ConfigOverrideJson = json;
        UpdateTimestamp();
    }

    public void SetPositionIndex(int index)
    {
        if (PositionIndex != index)
        {
            PositionIndex = index;
            UpdateTimestamp();
        }
    }
}
