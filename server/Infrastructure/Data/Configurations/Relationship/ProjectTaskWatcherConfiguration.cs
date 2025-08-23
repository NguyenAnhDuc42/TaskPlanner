using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Relationship;

namespace Infrastructure.Data.Configurations.Relationship;

public class ProjectTaskWatcherConfiguration : IEntityTypeConfiguration<ProjectTaskWatcher>
{
    public void Configure(EntityTypeBuilder<ProjectTaskWatcher> builder)
    {
        builder.ToTable("ProjectTaskWatchers");

        // Composite primary key
        builder.HasKey(ptw => new { ptw.ProjectTaskId, ptw.UserId });

        // Configure relationships
        builder.HasOne<Domain.Entities.ProjectWorkspace.ProjectTask>(ptw => ptw.ProjectTask)
            .WithMany(pt => pt.Watchers) // Assuming ProjectTask has a Watchers collection
            .HasForeignKey(ptw => ptw.ProjectTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Domain.Entities.User>(ptw => ptw.User)
            .WithMany()
            .HasForeignKey(ptw => ptw.UserId)
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
