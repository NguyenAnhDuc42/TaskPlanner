using System;
using Domain.Entities;
using Domain.Entities.ProjectEntities;

namespace Domain.Entities.Relationship;

public class UserProjectFolder
{
    public Guid UserId { get; set; }
    public Guid ProjectFolderId { get; set; }
    public DateTime JoinTime { get; private set; }

    // Navigation Properties
    public User User { get; private set; } = null!;
    public ProjectFolder ProjectFolder { get; private set; } = null!;

    private UserProjectFolder() { } // For EF Core

    private UserProjectFolder(Guid userId, Guid projectFolderId, DateTime joinTime)
    {
        UserId = userId;
        ProjectFolderId = projectFolderId;
        JoinTime = joinTime;
    }

    // Static Factory Method
    public static UserProjectFolder Create(Guid userId, Guid projectFolderId)
    {
        return new UserProjectFolder(userId, projectFolderId, DateTime.UtcNow);
    }
}