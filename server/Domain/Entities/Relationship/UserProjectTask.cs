using Domain.Entities.ProjectWorkspace;
using System;
using Domain.Entities;

namespace Domain.Entities.Relationship;

public class UserProjectTask
{
    public Guid UserId { get; set; }
    public User User { get; private set; } = null!;
    public Guid ProjectTaskId { get; set; }
    public ProjectTask ProjectTask { get; private set; } = null!;

    public DateTime JoinTime { get; private set; }

    private UserProjectTask() { } // For EF Core

    private UserProjectTask(Guid userId, Guid projectTaskId, DateTime joinTime)
    {
        UserId = userId;
        ProjectTaskId = projectTaskId;
        JoinTime = joinTime;
    }

    // Static Factory Method
    public static UserProjectTask Create(Guid userId, Guid projectTaskId)
    {
        return new UserProjectTask(userId, projectTaskId, DateTime.UtcNow);
    }
}