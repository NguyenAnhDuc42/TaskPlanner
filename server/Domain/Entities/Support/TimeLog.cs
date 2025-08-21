using Domain.Common;
using System;

namespace Domain.Entities.Support;

public class TimeLog : Entity
{
    public TimeSpan TimeSpent { get; private set; }
    public DateTimeOffset LogDate { get; private set; }
    public Guid UserId { get; private set; }
    public Guid ProjectTaskId { get; private set; }

    private TimeLog() { } // For EF Core

    private TimeLog(Guid id, TimeSpan timeSpent, Guid userId, Guid projectTaskId)
    {
        Id = id;
        TimeSpent = timeSpent;
        LogDate = DateTimeOffset.UtcNow;
        UserId = userId;
        ProjectTaskId = projectTaskId;
    }

    public static TimeLog Create(TimeSpan timeSpent, Guid userId, Guid projectTaskId)
    {
        if (timeSpent <= TimeSpan.Zero)
            throw new ArgumentException("Time spent must be greater than zero.", nameof(timeSpent));

        return new TimeLog(Guid.NewGuid(), timeSpent, userId, projectTaskId);
    }
}