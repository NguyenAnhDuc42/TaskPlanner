using System;
using src.Domain.Entities.UserEntity;

namespace src.Domain.Entities.WorkspaceEntity.Relationships;

public class UserList
{
    public Guid UserId { get; set; }
    public Guid ListId { get; set; }
}
