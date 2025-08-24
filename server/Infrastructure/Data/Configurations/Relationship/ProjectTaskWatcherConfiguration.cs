using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Relationship;
using Domain.Entities.ProjectEntities; // Add this using statement for ProjectTask

namespace Infrastructure.Data.Configurations.Relationship;

public class ProjectTaskWatcherConfiguration : IEntityTypeConfiguration<ProjectTaskWatcher>
{
    public void Configure(EntityTypeBuilder<ProjectTaskWatcher> builder)
    {
        builder.ToTable("ProjectTaskWatchers");

        // Composite primary key
        builder.HasKey(ptw => new { ptw.ProjectTaskId, ptw.UserId });

        // Configure relationships
        builder.HasOne<ProjectTask>(ptw => ptw.ProjectTask)
            .WithMany(pt => pt.Watchers)
            .HasForeignKey(ptw => ptw.ProjectTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Domain.Entities.User>(ptw => ptw.User)
            .WithMany()
            .HasForeignKey(ptw => ptw.UserId)
            .OnDelete(DeleteBehavior.Cascade);

    // ProjectTaskWatcher is a relationship/POCO and does not have Version/CreatedAt/UpdatedAt properties
    }
}
