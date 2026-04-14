using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Data.Configurations;

public class ProjectSpaceConfiguration : EntityConfiguration<ProjectSpace>
{
    public override void Configure(EntityTypeBuilder<ProjectSpace> builder)
    {
        base.Configure(builder);

        builder.ToTable("project_spaces");

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .HasColumnOrder(0);

        builder.Property(s => s.ProjectWorkspaceId)
            .HasColumnName("project_workspace_id")
            .HasColumnOrder(1);

        builder.Property(s => s.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(150)
            .HasColumnOrder(2);

        builder.Property(s => s.Slug)
            .HasColumnName("slug")
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnOrder(3);

        builder.HasIndex(s => new { s.ProjectWorkspaceId, s.Slug }).IsUnique();

        builder.Property(s => s.Description)
            .HasColumnName("description")
            .HasColumnType("jsonb")
            .HasColumnOrder(4);

        builder.OwnsOne(s => s.Customization, c =>
        {
            c.Property(cust => cust.Color).HasColumnName("custom_color").HasColumnOrder(5);
            c.Property(cust => cust.Icon).HasColumnName("custom_icon").HasColumnOrder(6);
        });

        builder.Property(s => s.IsPrivate)
            .HasColumnName("is_private")
            .HasColumnOrder(7);

        builder.Property(s => s.OrderKey)
            .HasColumnName("order_key")
            .IsRequired()
            .HasColumnOrder(8);

        builder.Property(s => s.IsArchived)
            .HasColumnName("is_archived")
            .HasColumnOrder(9);

        builder.Property(w => w.WorkflowId)
            .HasColumnName("workflow_id")
            .HasColumnOrder(10);

        builder.Property(w => w.StatusId)
            .HasColumnName("status_id")
            .HasColumnOrder(11);

        builder.HasIndex(w => w.WorkflowId);
        builder.HasIndex(w => w.StatusId);

        // Auditing (Overrides from base to set order)
        builder.Property(w => w.CreatedAt).HasColumnOrder(12);
        builder.Property(w => w.UpdatedAt).HasColumnOrder(13);
        builder.Property(w => w.DeletedAt).HasColumnOrder(14);
        builder.Property(w => w.CreatorId).HasColumnOrder(15);
    }
}
