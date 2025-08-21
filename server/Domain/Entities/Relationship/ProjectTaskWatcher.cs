using System;
using Domain.Entities;
using Domain.Entities.ProjectWorkspace;

namespace Domain.Entities.Relationship;

public class ProjectTaskWatcher
{
    public Guid ProjectTaskId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinTime { get; private set; }

    // Navigation Properties
    public ProjectTask ProjectTask { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private ProjectTaskWatcher() { } // For EF Core

    private ProjectTaskWatcher(Guid projectTaskId, Guid userId, DateTime joinTime)
    {
        ProjectTaskId = projectTaskId;
        UserId = userId;
        JoinTime = joinTime;
    }

    // Static Factory Method
    public static ProjectTaskWatcher Create(Guid projectTaskId, Guid userId)
    {
        return new ProjectTaskWatcher(projectTaskId, userId, DateTime.UtcNow);
    }
}