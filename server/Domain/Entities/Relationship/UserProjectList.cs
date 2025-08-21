using System;

namespace Domain.Entities.Relationship;

public class UserProjectList
{
    public Guid UserId { get; set; }
    public Guid ProjectListId { get; set; }
}
