using System;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Support;

namespace Domain.Entities.Relationship;

public class ProjectTaskTag
{
    public Guid ProjectTaskId { get; set; }
    public ProjectTask ProjectTask { get; set; } = null!;

    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;

    private ProjectTaskTag() { } // For EF Core

    public ProjectTaskTag(Guid projectTaskId, Guid tagId)
    {
        ProjectTaskId = projectTaskId;
        TagId = tagId;
    }
}
