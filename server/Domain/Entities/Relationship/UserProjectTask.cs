using Domain.Entities.ProjectWorkspace;
using System;

namespace Domain.Entities.Relationship;

public class UserProjectTask
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid ProjectTaskId { get; set; }
    public ProjectTask? Task { get; set; }
}
