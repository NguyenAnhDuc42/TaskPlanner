using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Data.Configurations;

public class ProjectFolderConfiguration : EntityConfiguration<ProjectFolder>
{
    public override void Configure(EntityTypeBuilder<ProjectFolder> builder)
    {
        base.Configure(builder);

        builder.ToTable("project_folders");

        builder.Property(f => f.Id)
            .HasColumnName("id")
            .HasColumnOrder(0);

        builder.Property(f => f.ProjectWorkspaceId)
            .HasColumnName("project_workspace_id")
            .HasColumnOrder(1);

        builder.Property(f => f.ProjectSpaceId)
            .HasColumnName("project_space_id")
            .HasColumnOrder(2);

        builder.Property(f => f.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(150)
            .HasColumnOrder(3);

        builder.Property(f => f.Slug)
            .HasColumnName("slug")
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnOrder(4);

        builder.HasIndex(f => new { f.ProjectSpaceId, f.Slug }).IsUnique();

        builder.Property(f => f.Description)
            .HasColumnName("description")
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null!),
                v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<string>(v, (System.Text.Json.JsonSerializerOptions)null!)
            )
            .HasColumnOrder(5);

        builder.Property(f => f.OrderKey)
            .HasColumnName("order_key")
            .IsRequired()
            .HasColumnOrder(6);

        builder.Property(f => f.IsPrivate)
            .HasColumnName("is_private")
            .HasColumnOrder(7);

        builder.Property(f => f.IsArchived)
            .HasColumnName("is_archived")
            .HasColumnOrder(8);

        builder.Property(f => f.StartDate)
            .HasColumnName("start_date")
            .HasColumnOrder(9);

        builder.Property(f => f.DueDate)
            .HasColumnName("due_date")
            .HasColumnOrder(10);

        builder.OwnsOne(f => f.Customization, c =>
        {
            c.Property(cust => cust.Color).HasColumnName("custom_color").HasColumnOrder(11);
            c.Property(cust => cust.Icon).HasColumnName("custom_icon").HasColumnOrder(12);
        });

        builder.Property(f => f.WorkflowId)
            .HasColumnName("workflow_id")
            .HasColumnOrder(13);

        builder.Property(f => f.StatusId)
            .HasColumnName("status_id")
            .HasColumnOrder(14);

        builder.HasIndex(f => f.WorkflowId);
        builder.HasIndex(f => f.StatusId);

        // Auditing (Overrides from base to set order)
        builder.Property(f => f.CreatedAt).HasColumnName("created_at").HasColumnOrder(15);
        builder.Property(f => f.UpdatedAt).HasColumnName("updated_at").HasColumnOrder(16);
        builder.Property(f => f.DeletedAt).HasColumnName("deleted_at").HasColumnOrder(17);
        builder.Property(f => f.CreatorId).HasColumnName("creator_id").HasColumnOrder(18);

        builder.HasIndex(f => new { f.ProjectWorkspaceId, f.ProjectSpaceId, f.OrderKey, f.Id })
            .HasFilter("\"deleted_at\" IS NULL AND \"is_archived\" = false")
            .IncludeProperties(f => new { f.Name, f.IsPrivate });
    }
}
