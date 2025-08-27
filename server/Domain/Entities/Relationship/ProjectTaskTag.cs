using System;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Support;

namespace Domain.Entities.Relationship
{
    /// <summary>
    /// Many-to-many join between ProjectTask and Tag.
    /// Composite key: (ProjectTaskId, TagId). Configure in EF Core.
    /// </summary>
    public class ProjectTaskTag
    {
        public Guid ProjectTaskId { get; private set; }
        public ProjectTask ProjectTask { get; private set; } = null!;
        public Guid TagId { get; private set; }
        public Tag Tag { get; private set; } = null!;

        public DateTime CreatedAt { get; private set; }

        private ProjectTaskTag() { } // EF Core
        private ProjectTaskTag(Guid projectTaskId, Guid tagId)
        {
            if (projectTaskId == Guid.Empty) throw new ArgumentException("Value cannot be empty.", nameof(projectTaskId));
            if (tagId == Guid.Empty) throw new ArgumentException("Value cannot be empty.", nameof(tagId));

            ProjectTaskId = projectTaskId;
            TagId = tagId;
            CreatedAt = DateTime.UtcNow;
        }

        public static ProjectTaskTag Create(Guid projectTaskId, Guid tagId) =>
            new ProjectTaskTag(projectTaskId, tagId);
    }
}
