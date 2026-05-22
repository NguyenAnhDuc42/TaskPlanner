using System.Text.Json.Serialization;

namespace Domain;

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

