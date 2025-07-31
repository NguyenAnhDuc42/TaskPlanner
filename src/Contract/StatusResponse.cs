namespace src.Contract;

/// <summary>
/// Represents a summary of the workload, typically for a workspace, space, or list.
/// It provides a breakdown of tasks by their status.
/// </summary>
/// <param name="TotalTaskCount">The total number of tasks included in this summary.</param>
/// <param name="StatusBreakdown">A list containing the count and percentage of tasks for each status.</param>
public record WorkloadSummaryResponse(int TotalTaskCount, List<StatusWorkloadSummary> StatusBreakdown);

/// <summary>
/// Provides a detailed summary for a single status within a workload breakdown.
/// </summary>
/// <param name="StatusId">The unique identifier for the status.</param>
/// <param name="Name">The name of the status (e.g., "To Do", "In Progress").</param>
/// <param name="Color">The color associated with the status, for UI rendering.</param>
/// <param name="TaskCount">The number of tasks currently in this status.</param>
/// <param name="Percentage">The percentage of the total tasks that are in this status (0-100).</param>
public record StatusWorkloadSummary(Guid StatusId, string Name, string Color, int TaskCount, double Percentage);
