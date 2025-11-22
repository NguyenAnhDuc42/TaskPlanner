using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Relationship;

namespace Infrastructure.Data.Configurations.Relationship;

public class TaskAssignmentConfiguration : CompositeConfiguration<TaskAssignment>
{
    public override void Configure(EntityTypeBuilder<TaskAssignment> builder)
    {
        base.Configure(builder);

        builder.ToTable("task_assignments");

        // Composite PK: TaskId + AssigneeId (a task can have many assignees but avoid dupes)
        builder.HasKey(x => new { x.TaskId, x.AssigneeId });

        builder.Property(x => x.TaskId).HasColumnName("task_id").IsRequired();
        builder.Property(x => x.AssigneeId).HasColumnName("assignee_id").IsRequired();
        builder.Property(x => x.AssignedById).HasColumnName("assigned_by_id").IsRequired();

        // Indexes
        builder.HasIndex(x => x.TaskId);
        builder.HasIndex(x => x.AssigneeId);
    }
}
