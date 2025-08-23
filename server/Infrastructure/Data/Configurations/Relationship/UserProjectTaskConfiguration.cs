using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Relationship;

namespace Infrastructure.Data.Configurations.Relationship;

public class UserProjectTaskConfiguration : IEntityTypeConfiguration<UserProjectTask>
{
    public void Configure(EntityTypeBuilder<UserProjectTask> builder)
    {
        builder.ToTable("UserProjectTasks");

        // Composite primary key
        builder.HasKey(upt => new { upt.UserId, upt.ProjectTaskId });

        // Configure relationships
        builder.HasOne<Domain.Entities.User>(upt => upt.User)
            .WithMany()
            .HasForeignKey(upt => upt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Domain.Entities.ProjectWorkspace.ProjectTask>(upt => upt.ProjectTask)
            .WithMany(pt => pt.Assignees) // Assuming ProjectTask has an Assignees collection
            .HasForeignKey(upt => upt.ProjectTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure common Entity properties
        builder.Property(e => e.Version)
            .IsRowVersion();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();
    }
}
