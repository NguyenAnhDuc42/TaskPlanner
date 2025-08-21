using Domain.Enums;
using System;

namespace Domain.Entities.Relationship;

public class UserProjectWorkspace
{
    public Guid UserId { get; set; }
    public Guid ProjectWorkspaceId { get; set; }
    public Role Role { get; set; }

    public UserProjectWorkspace() { }
    public UserProjectWorkspace(Guid userId, Guid projectWorkspaceId, Role role)
    {
        UserId = userId;
        ProjectWorkspaceId = projectWorkspaceId;
        Role = role;
    }
}
