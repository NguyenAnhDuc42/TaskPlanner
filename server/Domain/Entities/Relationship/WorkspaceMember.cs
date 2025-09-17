using System;
using System.ComponentModel.DataAnnotations;
using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Relationship;

public class WorkspaceMember : Composite
{
    [Required] public Guid UserId { get; private set; }
    [Required] public Guid ProjectWorkspaceId { get; private set; }
    [Required] public Role Role { get; private set; } // Only here
    public bool IsPending { get; private set; }
    private WorkspaceMember() { } // EF

    private WorkspaceMember(Guid userId, Guid workspaceId, Role role)
    {
        UserId = userId;
        ProjectWorkspaceId = workspaceId;
        Role = role;
    }

    public static WorkspaceMember Create(Guid userId, Guid workspaceId, Role role ,bool isPending = false)
        => new(userId, workspaceId, role);

    public void UpdateRole(Role newRole) => Role = newRole;
}
