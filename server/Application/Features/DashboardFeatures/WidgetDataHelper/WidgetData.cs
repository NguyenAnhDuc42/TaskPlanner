
using System.Text.Json.Serialization;

namespace Application.Features.DashboardFeatures.WidgetDataHelper;

/// <summary>
/// Represents the layout position and dimensions of a widget.
/// </summary>
public record WidgetPosition(int Col, int Row, int Width, int Height);

/// <summary>
/// High-level base class for all widget data results.
/// Uses the Discriminated Union pattern for polymorphic JSON serialization.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TaskStatusWidgetData), "TaskList")]
[JsonDerivedType(typeof(FolderListWidgetData), "FolderList")]
[JsonDerivedType(typeof(ActivityFeedWidgetData), "ActivityFeed")]
[JsonDerivedType(typeof(HealthCheckWidgetData), "WorkspaceHealth")]
public abstract class WidgetData
{
    public Guid WidgetId { get; set; }
    public WidgetPosition Position { get; set; } = null!;
}

/// <summary>
/// Specialized data for Task List widgets.
/// </summary>
public class TaskStatusWidgetData : WidgetData
{
    public int TotalCount { get; set; }
    public int OverdueCount { get; set; }
    public int TodayCount { get; set; }
    public List<TaskStatusItem> Tasks { get; set; } = new();
}

/// <summary>
/// Placeholder for Health Check widget data.
/// </summary>
public class HealthCheckWidgetData : WidgetData
{
    public List<object> HealthChecks { get; set; } = new();
}

/// <summary>
/// Placeholder for Goals progress widget data.
/// </summary>
public class GoalsWidgetData : WidgetData
{
    public List<object> Goals { get; set; } = new();
}

public class FolderListWidgetData : WidgetData
{
    public List<object> Folders { get; set; } = new();
}

public class ActivityFeedWidgetData : WidgetData
{
    public List<object> Activities { get; set; } = new();
}
