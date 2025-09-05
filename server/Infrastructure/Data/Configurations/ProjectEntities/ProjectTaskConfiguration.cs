using Domain.Entities.ProjectEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.ProjectEntities;

public class ProjectTaskConfiguration : IEntityTypeConfiguration<ProjectTask>
{
    public void Configure(EntityTypeBuilder<ProjectTask> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(t => t.Id);

        // Add indexes for frequently queried columns
        builder.HasIndex(t => t.ProjectListId);
        builder.HasIndex(t => t.StatusId);
        builder.HasIndex(t => t.DueDate);

        // Properties
        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(1000);

        builder.Property(t => t.CreatorId)
            .IsRequired();

        builder.Property(t => t.IsCompleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.IsArchived)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.Priority)
            .IsRequired();

        builder.Property(t => t.Visibility)
            .IsRequired();

        builder.Property(t => t.StartDate); // Added
        builder.Property(t => t.DueDate); // Added
        builder.Property(t => t.StoryPoints); // Added
        builder.Property(t => t.TimeEstimateSeconds); // Added
        builder.Property(t => t.OrderKey); // Added
        builder.Property(t => t.StatusId); // Added

        // Relationships
        builder.HasOne<ProjectList>()
            .WithMany()
            .HasForeignKey(t => t.ProjectListId)
            .OnDelete(DeleteBehavior.Cascade);

        // Self-referencing relationship for subtasks
        builder.HasOne<ProjectTask>()
            .WithMany(t => t.Subtasks)
            .HasForeignKey(t => t.ParentTaskId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict); // Prevent parent task deletion if subtasks exist

        // Relationships to join tables
        builder.HasMany(t => t.Assignees)
            .WithOne(a => a.ProjectTask)
            .HasForeignKey(a => a.ProjectTaskId)
            .OnDelete(DeleteBehavior.Cascade);


        builder.HasMany(t => t.Tags)
            .WithOne(t => t.ProjectTask)
            .HasForeignKey(t => t.ProjectTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationships to owned support entities
        builder.HasMany(t => t.Comments)
            .WithOne()
            .HasForeignKey(c => c.ProjectTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Attachments)
            .WithOne()
            .HasForeignKey(a => a.ProjectTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.TimeLogs)
            .WithOne()
            .HasForeignKey(tl => tl.ProjectTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Checklists)
            .WithOne()
            .HasForeignKey(cl => cl.ProjectTaskId)
            .OnDelete(DeleteBehavior.Cascade);

       
        // Ignore DomainEvents
        builder.Ignore(t => t.DomainEvents);
    }
}