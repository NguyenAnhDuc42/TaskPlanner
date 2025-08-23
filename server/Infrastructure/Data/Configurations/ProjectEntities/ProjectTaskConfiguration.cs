using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship; // For UserProjectTask, ProjectTaskWatcher
using Domain.Entities.Support; // For Tag, Attachment, Comment, TimeLog, Checklist
using Domain.Enums; // For Priority

namespace Infrastructure.Data.Configurations.ProjectEntities;

public class ProjectTaskConfiguration : IEntityTypeConfiguration<ProjectTask>
{
    public void Configure(EntityTypeBuilder<ProjectTask> builder)
    {
        builder.ToTable("ProjectTasks");

        builder.HasKey(pt => pt.Id);

        builder.Property(pt => pt.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(pt => pt.Description)
            .HasMaxLength(2048); // Increased length for description

        builder.Property(pt => pt.Priority)
            .IsRequired()
            .HasConversion<string>(); // Store enum as string

        builder.Property(pt => pt.DueDate)
            .IsRequired(false);

        builder.Property(pt => pt.StartDate)
            .IsRequired(false);

        builder.Property(pt => pt.TimeEstimate)
            .IsRequired(false);

        builder.Property(pt => pt.TimeSpent)
            .IsRequired(false);

        builder.Property(pt => pt.StoryPoints)
            .IsRequired(false);

        builder.Property(pt => pt.OrderIndex)
            .IsRequired();

        builder.Property(pt => pt.IsArchived)
            .IsRequired();

        builder.Property(pt => pt.IsPrivate)
            .IsRequired();

        builder.Property(pt => pt.CreatorId)
            .IsRequired();

        builder.Property(pt => pt.ProjectWorkspaceId)
            .IsRequired();

        builder.Property(pt => pt.ProjectSpaceId)
            .IsRequired();

        builder.Property(pt => pt.ProjectListId)
            .IsRequired();

        builder.Property(pt => pt.ProjectFolderId)
            .IsRequired(false); // Nullable

        builder.Property(pt => pt.StatusId)
            .IsRequired(false); // Nullable

        builder.Property(pt => pt.ParentTaskId)
            .IsRequired(false); // Nullable

        // Configure relationships
        builder.HasMany(pt => pt.Assignees)
            .WithOne()
            .HasForeignKey(upa => upa.ProjectTaskId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(pt => pt.Watchers)
            .WithOne()
            .HasForeignKey(ptw => ptw.ProjectTaskId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(pt => pt.Tags)
            .WithOne()
            .HasForeignKey(t => t.ProjectTaskId) // Assuming Tag has ProjectTaskId
            .IsRequired(false) // Tags might exist independently or be associated
            .OnDelete(DeleteBehavior.SetNull); // If task is deleted, tags might remain

        builder.HasMany(pt => pt.Attachments)
            .WithOne()
            .HasForeignKey(a => a.ProjectTaskId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(pt => pt.Comments)
            .WithOne()
            .HasForeignKey(c => c.ProjectTaskId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(pt => pt.TimeLogs)
            .WithOne()
            .HasForeignKey(tl => tl.ProjectTaskId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(pt => pt.Checklists)
            .WithOne()
            .HasForeignKey(cl => cl.ProjectTaskId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // Configure common Entity properties
        builder.Property(e => e.Version)
            .IsRowVersion();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        // Ignore domain events collection as it's not persisted
        builder.Ignore(e => e.DomainEvents);
    }
}
