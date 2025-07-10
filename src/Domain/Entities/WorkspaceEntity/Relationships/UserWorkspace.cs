using System;
using src.Domain.Entities.UserEntity;
using src.Domain.Enums;

namespace src.Domain.Entities.WorkspaceEntity.Relationships;

public class UserWorkspace
{
    public Guid UserId { get; set; }
    public required User User { get; set; }
    public Guid WorkspaceId { get; set; }
    public required Workspace Workspace { get; set; }
    public Role Role { get; set; }
}
