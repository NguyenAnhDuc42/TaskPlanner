using Domain.Enums;
using System;
using Domain.Entities;
using Domain.Entities.ProjectEntities;

namespace Domain.Entities.Relationship;

public class UserProjectWorkspace
{
    public Guid UserId { get; set; }
    public Guid ProjectWorkspaceId { get; set; }
    public Role Role { get; set; }
    public DateTime JoinTime { get; private set; }

    // Navigation Properties
    public User User { get; private set; } = null!;
    public ProjectWorkspace ProjectWorkspace { get; private set; } = null!;

    private UserProjectWorkspace() { } // For EF Core

    private UserProjectWorkspace(Guid userId, Guid projectWorkspaceId, Role role, DateTime joinTime)
    {
        UserId = userId;
        ProjectWorkspaceId = projectWorkspaceId;
        Role = role;
        JoinTime = joinTime;
    }

    // Static Factory Method
    public static UserProjectWorkspace Create(Guid userId, Guid projectWorkspaceId, Role role)
    {
        return new UserProjectWorkspace(userId, projectWorkspaceId, role, DateTime.UtcNow);
    }

    public void UpdateRole(Role newRole)
    {
        if (Role == newRole) return;
        Role = newRole;
    }
}