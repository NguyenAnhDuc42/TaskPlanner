using System;

namespace Domain.Entities.Relationship;

public class UserProjectSpace
{
    public Guid UserId { get; set; }
    public Guid ProjectSpaceId { get; set; }
}
