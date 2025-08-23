using System;
using Domain.Entities;
using Domain.Entities.ProjectEntities;

namespace Domain.Entities.Relationship;

public class UserProjectList
{
    public Guid UserId { get; set; }
    public Guid ProjectListId { get; set; }
    public DateTime JoinTime { get; private set; }

    // Navigation Properties
    public User User { get; private set; } = null!;
    public ProjectList ProjectList { get; private set; } = null!;

    private UserProjectList() { } // For EF Core

    private UserProjectList(Guid userId, Guid projectListId, DateTime joinTime)
    {
        UserId = userId;
        ProjectListId = projectListId;
        JoinTime = joinTime;
    }

    // Static Factory Method
    public static UserProjectList Create(Guid userId, Guid projectListId)
    {
        return new UserProjectList(userId, projectListId, DateTime.UtcNow);
    }
}