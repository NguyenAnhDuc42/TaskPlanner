using Domain.Common;

namespace Domain.Entities;

public class TaskAssignment : Entity
{
    public Guid ProjectTaskId { get; private set; }
    public Guid WorkspaceMemberId { get; private set; }
    public string? Notes { get; private set; }
    public int? EstimatedHours { get; private set; }
    public int? ActualHours { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private TaskAssignment() { }

    private TaskAssignment(Guid projectTaskId, Guid workspaceMemberId, Guid creatorId, string? notes = null, int? estimatedHours = null)
        : base(Guid.NewGuid())
    {
        ProjectTaskId = projectTaskId;
        WorkspaceMemberId = workspaceMemberId;
        Notes = notes;
        EstimatedHours = estimatedHours;
        
        // Audit is initialized in base constructor
        InitializeAudit(creatorId);
    }

    public static TaskAssignment Create(Guid projectTaskId, Guid workspaceMemberId, Guid creatorId, string? notes = null, int? estimatedHours = null) =>
        new(projectTaskId, workspaceMemberId, creatorId, notes, estimatedHours);

    public void MarkCompleted(int? actualHours = null)
    {
        CompletedAt = DateTimeOffset.UtcNow;
        if (actualHours.HasValue) ActualHours = actualHours;
        UpdateTimestamp();
    }

    public void UpdateNotes(string notes)
    {
        Notes = notes;
        UpdateTimestamp();
    }

    public void UpdateEstimatedHours(int hours)
    {
        if (hours <= 0) throw new ArgumentException("Hours must be positive");
        EstimatedHours = hours;
        UpdateTimestamp();
    }

    public bool IsCompleted => CompletedAt.HasValue;
}
