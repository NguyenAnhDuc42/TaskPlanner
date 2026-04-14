using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Data.Configurations;

public class ProjectTaskConfiguration : EntityConfiguration<ProjectTask>
{   
    public override void Configure(EntityTypeBuilder<ProjectTask> builder)
    {
        base.Configure(builder);

        builder.ToTable("project_tasks");

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .HasColumnOrder(0);

        builder.Property(t => t.ProjectWorkspaceId)
            .HasColumnName("project_workspace_id")
            .IsRequired()
            .HasColumnOrder(1);

        builder.Property(t => t.ProjectSpaceId)
            .HasColumnName("project_space_id")
            .HasColumnOrder(2);

        builder.Property(t => t.ProjectFolderId)
            .HasColumnName("project_folder_id")
            .HasColumnOrder(3);

        builder.Property(t => t.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnOrder(4);

        builder.Property(t => t.Slug)
            .HasColumnName("slug")
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnOrder(5);

        builder.HasIndex(t => new { t.ProjectWorkspaceId, t.Slug }).IsUnique();

        builder.Property(t => t.Description)
            .HasColumnName("description")
            .HasColumnType("jsonb")
            .HasColumnOrder(6);

        builder.Property(t => t.StatusId)
            .HasColumnName("status_id")
            .HasColumnOrder(7);

        builder.Property(t => t.Priority)
            .HasColumnName("priority")
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasColumnOrder(8);

        builder.Property(t => t.OrderKey)
            .HasColumnName("order_key")
            .IsRequired()
            .HasColumnOrder(9);

        builder.Property(t => t.StartDate)
            .HasColumnName("start_date")
            .HasColumnOrder(10);

        builder.Property(t => t.DueDate)
            .HasColumnName("due_date")
            .HasColumnOrder(11);

        builder.Property(t => t.StoryPoints)
            .HasColumnName("story_points")
            .HasColumnOrder(12);

        builder.Property(t => t.TimeEstimate)
            .HasColumnName("time_estimate")
            .HasColumnOrder(13);

        builder.Property(t => t.IsArchived)
            .HasColumnName("is_archived")
            .HasColumnOrder(14);

        // Auditing (Overrides from base to set order)
        builder.Property(t => t.CreatedAt).HasColumnOrder(15);
        builder.Property(t => t.UpdatedAt).HasColumnOrder(16);
        builder.Property(t => t.DeletedAt).HasColumnOrder(17);
        builder.Property(t => t.CreatorId).HasColumnOrder(18);

        builder.OwnsOne(x => x.Customization, cb =>
        {
            cb.Property(p => p.Color).HasColumnName("custom_color").IsRequired().HasColumnOrder(19);
            cb.Property(p => p.Icon).HasColumnName("custom_icon").IsRequired().HasColumnOrder(20);
        });

        builder.HasIndex(x => x.ProjectWorkspaceId);
        builder.HasIndex(x => x.ProjectSpaceId);
        builder.HasIndex(x => x.ProjectFolderId);
        builder.HasIndex(x => new { x.ProjectSpaceId, x.StatusId });
        builder.HasIndex(x => x.DueDate);
    }
}
