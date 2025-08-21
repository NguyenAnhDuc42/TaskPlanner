using System;

namespace Domain.Entities.Relationship;

public class ProjectTaskWatcher
{
    public Guid ProjectTaskId { get; set; }
    public Guid UserId { get; set; }
}
