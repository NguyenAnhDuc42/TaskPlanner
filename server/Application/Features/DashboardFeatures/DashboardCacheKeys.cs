using System;

namespace Application.Features.DashboardFeatures;

public static class DashboardCacheKeys
{
    public static string DashboardListTag(Guid layerId) => $"layer:{layerId}:dashboards";

    public static string DashboardList(Guid layerId) => $"{DashboardListTag(layerId)}:list";
}
