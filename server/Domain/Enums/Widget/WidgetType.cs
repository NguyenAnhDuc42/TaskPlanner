using System.Text.Json.Serialization;

namespace Domain.Enums.Widget;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WidgetType
{
    TaskList,
    FolderList,
    ActivityFeed,
    NotificationSummary,
    UpcomingDeadlines,

    // Insights / analytics
    WorkspaceHealth,
    WorkloadSummary,
    GoalProgress,

    // Utility
    QuickActions,
    Calendar,

    // Visual / optional
    Hero,

}
