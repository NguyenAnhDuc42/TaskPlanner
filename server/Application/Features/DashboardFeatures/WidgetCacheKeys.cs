using System;

namespace Application.Features.DashboardFeatures;

public static class WidgetCacheKeys
{
    public static string WidgetListTag(Guid dashboardId) => $"dashboard:{dashboardId}:widgets";

    public static string WidgetList(Guid dashboardId) => $"{WidgetListTag(dashboardId)}:list";
}
