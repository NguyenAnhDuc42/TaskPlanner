using System;

namespace Domain.Entities.Relationship;

public class UserProjectFolder
{
    public Guid UserId { get; set; }
    public Guid ProjectFolderId { get; set; }
}
