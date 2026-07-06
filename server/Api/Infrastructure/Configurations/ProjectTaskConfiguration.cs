using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Api;

public class ProjectTaskConfiguration : TenantEntityConfiguration<ProjectTask>
{
    public override void Configure(EntityTypeBuilder<ProjectTask> builder)
    {
        base.Configure(builder);

        builder.ToTable("project_tasks");

        builder.Property(t => t.Id)
            .HasColumnName("id");

        builder.Property(t => t.ProjectWorkspaceId)
            .HasColumnName("project_workspace_id");

        builder.Property(t => t.ProjectSpaceId)
            .HasColumnName("project_space_id");

        builder.Property(t => t.ProjectFolderId)
            .HasColumnName("project_folder_id");

        builder.Property(t => t.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(t => t.Slug)
            .HasColumnName("slug")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.DefaultDocumentId)
            .HasColumnName("default_document_id")
            .IsRequired();

        builder.Property(t => t.Color)
            .HasColumnName("custom_color")
            .HasMaxLength(16);

        builder.Property(t => t.Icon)
            .HasColumnName("custom_icon")
            .HasMaxLength(64);

        builder.Property(t => t.StatusId)
            .HasColumnName("status_id");

        builder.Property(t => t.IsArchived)
            .HasColumnName("is_archived");

        builder.Property(t => t.Priority)
            .HasColumnName("priority");

        builder.Property(t => t.StartDate).HasColumnName("start_date");
        builder.Property(t => t.DueDate).HasColumnName("due_date");

        builder.Property(t => t.StoryPoints).HasColumnName("story_points");
        builder.Property(t => t.TimeEstimateSeconds).HasColumnName("time_estimate_seconds");

        builder.Property(t => t.OrderKey)
            .HasColumnName("order_key")
            .IsRequired();

        builder.Property(t => t.ParentTaskId)
            .HasColumnName("parent_task_id");

        builder.HasOne<ProjectTask>()
            .WithMany()
            .HasForeignKey(t => t.ParentTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign Keys
        builder.HasOne<ProjectSpace>()
            .WithMany()
            .HasForeignKey(t => t.ProjectSpaceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<ProjectFolder>()
            .WithMany()
            .HasForeignKey(t => t.ProjectFolderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Status>()
            .WithMany()
            .HasForeignKey(t => t.StatusId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.StatusId);
        builder.HasIndex(t => t.ProjectSpaceId);
        builder.HasIndex(t => t.ProjectFolderId);

        // Cursor pagination: tasks within a folder
        builder.HasIndex(t => new { t.ProjectFolderId, t.OrderKey, t.Id })
            .HasFilter("deleted_at IS NULL AND is_archived = false")
            .HasDatabaseName("IX_project_tasks_folder_order_key");

        // Cursor pagination: tasks directly in a space (no folder)
        builder.HasIndex(t => new { t.ProjectWorkspaceId, t.ProjectSpaceId, t.OrderKey, t.Id })
            .HasFilter("deleted_at IS NULL AND is_archived = false AND project_folder_id IS NULL")
            .HasDatabaseName("IX_project_tasks_space_order_key");
    }
}


