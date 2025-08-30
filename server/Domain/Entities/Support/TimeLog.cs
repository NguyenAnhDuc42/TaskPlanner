using System;
using Domain.Common;

namespace Domain.Entities.Support;

public class TimeLog : Entity
{
    public Guid ProjectTaskId { get; private set; }
    public Guid UserId { get; private set; }
    public TimeSpan Duration { get; private set; }
    public string? Description { get; private set; }

    private TimeLog() { } // EF Core

    private TimeLog(Guid id, TimeSpan duration, Guid userId, Guid projectTaskId, string? description)
        : base(id)
    {
        if (duration <= TimeSpan.Zero) throw new ArgumentException("Duration must be greater than zero.", nameof(duration));
        if (userId == Guid.Empty) throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        if (projectTaskId == Guid.Empty) throw new ArgumentException("ProjectTaskId cannot be empty.", nameof(projectTaskId));

        Duration = duration;
        UserId = userId;
        ProjectTaskId = projectTaskId;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public static TimeLog Create(TimeSpan duration, Guid userId, Guid projectTaskId, string? description = null)
        => new TimeLog(Guid.NewGuid(), duration, userId, projectTaskId, description);

    public void UpdateDescription(string? description)
    {
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UpdateTimestamp();
    }
}
