using System;
using Domain.Entities;
using Domain.Entities.ProjectEntities;

namespace Domain.Entities.Relationship;

public class UserProjectSpace
{
    public Guid UserId { get; set; }
    public Guid ProjectSpaceId { get; set; }
    public DateTime JoinTime { get; private set; }

    // Navigation Properties
    public User User { get; private set; } = null!;
    public ProjectSpace ProjectSpace { get; private set; } = null!;

    private UserProjectSpace() { } // For EF Core

    private UserProjectSpace(Guid userId, Guid projectSpaceId, DateTime joinTime)
    {
        UserId = userId;
        ProjectSpaceId = projectSpaceId;
        JoinTime = joinTime;
    }

    // Static Factory Method
    public static UserProjectSpace Create(Guid userId, Guid projectSpaceId)
    {
        return new UserProjectSpace(userId, projectSpaceId, DateTime.UtcNow);
    }
}