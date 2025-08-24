using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Relationship;
using Domain.Entities.ProjectEntities; // Add this using statement for ProjectTask

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

        builder.HasOne<ProjectTask>(upt => upt.ProjectTask) // Corrected namespace
            .WithMany(pt => pt.Assignees) // Assuming ProjectTask has an Assignees collection
            .HasForeignKey(upt => upt.ProjectTaskId)
            .OnDelete(DeleteBehavior.Cascade);

    // UserProjectTask is a relationship POCO and does not have Version/CreatedAt/UpdatedAt
    }
}
