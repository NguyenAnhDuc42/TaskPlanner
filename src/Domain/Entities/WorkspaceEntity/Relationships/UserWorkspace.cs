using System;
using src.Domain.Entities.UserEntity;
using src.Domain.Enums;

namespace src.Domain.Entities.WorkspaceEntity.Relationships;

public class UserWorkspace
{
    public Guid UserId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Role Role { get; set; }

    public UserWorkspace() { }
    public UserWorkspace(Guid userId, Guid workspaceId, Role role)
    {
        UserId = userId;
        WorkspaceId = workspaceId;
        Role = role;
    }
    
}
