using System;
using src.Domain.Entities.UserEntity;

namespace src.Domain.Entities.WorkspaceEntity.Relationships;

public class UserSpace
{
    public Guid UserId { get; set; }
    public required User User { get; set; }
    public Guid SpaceId { get; set; }
    public required Space Space { get; set; }
}
