using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Relationship;
using Domain.Entities.ProjectEntities;

namespace Infrastructure.Data.Configurations.Relationship;

public class TaskAssignmentConfiguration : EntityConfiguration<TaskAssignment>
{
    public override void Configure(EntityTypeBuilder<TaskAssignment> builder)
    {
        base.Configure(builder);

        builder.ToTable("task_assignments");

        builder.Property(x => x.TaskId).HasColumnName("task_id").IsRequired();
        builder.Property(x => x.WorkspaceMemberId).HasColumnName("workspace_member_id").IsRequired();
        builder.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(500);
        builder.Property(x => x.EstimatedHours).HasColumnName("estimated_hours");
        builder.Property(x => x.ActualHours).HasColumnName("actual_hours");
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at");

        // Indexes
        builder.HasIndex(x => x.TaskId);
        builder.HasIndex(x => x.WorkspaceMemberId);
        // Foreign keys
        builder.HasOne<WorkspaceMember>()
            .WithMany()
            .HasForeignKey(x => x.WorkspaceMemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ProjectTask>()
            .WithMany(pt => pt.Assignees)
            .HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
