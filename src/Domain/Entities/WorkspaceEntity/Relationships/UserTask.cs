using System;
using src.Domain.Entities.UserEntity;

namespace src.Domain.Entities.WorkspaceEntity.Relationships;

public class UserTask
{
    public Guid UserId { get; set; }
    public required User User { get; set; } 
    public Guid TaskId { get; set; }
    public required PlanTask Task { get; set; }
}
