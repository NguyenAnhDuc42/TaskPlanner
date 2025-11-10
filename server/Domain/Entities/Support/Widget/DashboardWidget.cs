using System;
using Domain.Common;

namespace Domain.Entities.Support.Widget;

public class DashboardWidget : Composite
{
    public Guid DashboardId { get; private set; }
    public Guid WidgetId { get; private set; }
    public int Order { get; private set; }
    public string? DefaultConfigJson { get; private set; }

    private DashboardWidget() { } // EF

    internal DashboardWidget(Guid dashboardId, Guid widgetId, int order, string? defaultConfigJson)
    {
        DashboardId = dashboardId;
        WidgetId = widgetId;
        Order = order;
        DefaultConfigJson = defaultConfigJson;
    }

    internal void SetOrder(int order) { Order = order; }
}
