using System;
using src.Domain.Entities.UserEntity;

namespace src.Domain.Entities.WorkspaceEntity.Relationships;

public class UserTask
{
    public Guid UserId { get; set; }
    public User User { get; set; } 
    public Guid TaskId { get; set; }
    public PlanTask Task { get; set; }
}
