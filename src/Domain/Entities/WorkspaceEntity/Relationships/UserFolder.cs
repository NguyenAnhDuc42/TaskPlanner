using System;
using src.Domain.Entities.UserEntity;

namespace src.Domain.Entities.WorkspaceEntity.Relationships;

public class UserFolder
{
    public Guid UserId { get; set; }
    public Guid FolderId { get; set; }
}
