using System;
using Domain.Enums;

namespace Domain.Entities.Relationship;

public class WorkspaceMember
{
    public Guid UserId { get; private set; }
    public Guid ProjectWorkspaceId { get; private set; }
    public Role Role { get; private set; } // Only here
    public bool IsPending { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private WorkspaceMember() { } // EF

    private WorkspaceMember(Guid userId, Guid workspaceId, Role role)
    {
        UserId = userId;
        ProjectWorkspaceId = workspaceId;
        Role = role;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static WorkspaceMember Create(Guid userId, Guid workspaceId, Role role ,bool isPending = false)
        => new(userId, workspaceId, role);

    public void UpdateRole(Role newRole) => Role = newRole;
}
