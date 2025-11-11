using System;
using Domain.Common;

namespace Domain.Entities.Support.Widget;

public class DashboardWidget : Composite
{
    public Guid DashboardId { get; private set; }
    public Guid WidgetId { get; private set; }
    public int Order { get; private set; }
    public WidgetLayout Layout { get; private set; } = new WidgetLayout(0, 0, 2, 2);

    private DashboardWidget() { } // EF

    internal DashboardWidget(Guid dashboardId, Guid widgetId, int order, WidgetLayout? layout = null)
    {
        DashboardId = dashboardId;
        WidgetId = widgetId;
        Order = order;
        Layout = layout ?? new WidgetLayout(0, 0, 2, 2);
    }

    internal void SetOrder(int order) => Order = order;

    internal void UpdateLayout(WidgetLayout layout) => Layout = layout;
}