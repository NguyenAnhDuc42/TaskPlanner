using Domain.Common;
using System;

namespace Domain.Entities.Support;

public class TimeLog : Entity
{
    public Guid ProjectTaskId { get; private set; }
    public Guid UserId { get; private set; }
    public TimeSpan Duration { get; private set; }
    public string? Description { get; private set; }
    public DateTime LoggedAt { get; private set; }

    private TimeLog() { } // For EF Core

    private TimeLog(Guid id, TimeSpan duration, Guid userId, Guid projectTaskId)
    {
        Id = id;
        Duration = duration; // Assign to the existing Duration property
        UserId = userId;
        ProjectTaskId = projectTaskId;
        Description = null; // Initialize Description
        LoggedAt = DateTime.UtcNow; // Initialize LoggedAt
    }

    public static TimeLog Create(TimeSpan duration, Guid userId, Guid projectTaskId)
    {
        if (duration <= TimeSpan.Zero)
            throw new ArgumentException("Duration must be greater than zero.", nameof(duration));

        return new TimeLog(Guid.NewGuid(), duration, userId, projectTaskId);
    }
}