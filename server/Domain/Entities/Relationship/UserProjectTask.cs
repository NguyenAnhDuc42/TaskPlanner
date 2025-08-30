namespace Domain.Entities.Relationship;

using System;
using Domain.Entities.ProjectEntities;


public class UserProjectTask
{
    public Guid UserId { get; private set; }
    public User User { get; set; } = null!;
    public Guid ProjectTaskId { get; private set; }
    public ProjectTask ProjectTask { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private UserProjectTask() { } // EF

    private UserProjectTask(Guid userId, Guid taskId)
    {
        if (userId == Guid.Empty) throw new ArgumentException(nameof(userId));
        if (taskId == Guid.Empty) throw new ArgumentException(nameof(taskId));

        UserId = userId;
        ProjectTaskId = taskId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static UserProjectTask Create(Guid userId, Guid taskId)
        => new(userId, taskId);
}
