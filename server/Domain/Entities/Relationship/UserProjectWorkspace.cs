namespace Domain.Entities.Relationship;

using System;
using Domain.Entities.ProjectEntities;
using Domain.Enums;

public class UserProjectWorkspace
{
    public Guid UserId { get; private set; }
    public User User { get; set; } = null!;
    public Guid ProjectWorkspaceId { get; private set; }
    public ProjectWorkspace ProjectWorkspace { get; set; } = null!;
    public Role Role { get; private set; } // Only here
    public DateTime CreatedAt { get; private set; }

    private UserProjectWorkspace() { } // EF

    private UserProjectWorkspace(Guid userId, Guid workspaceId, Role role)
    {
        if (userId == Guid.Empty) throw new ArgumentException(nameof(userId));
        if (workspaceId == Guid.Empty) throw new ArgumentException(nameof(workspaceId));

        UserId = userId;
        ProjectWorkspaceId = workspaceId;
        Role = role;
        CreatedAt = DateTime.UtcNow;
    }

    public static UserProjectWorkspace Create(Guid userId, Guid workspaceId, Role role)
        => new(userId, workspaceId, role);

    public void UpdateRole(Role newRole) => Role = newRole;
}
