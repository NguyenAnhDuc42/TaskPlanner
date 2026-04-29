using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;
using Domain.Common;

namespace Infrastructure.Data.Configurations;

public class ProjectSpaceConfiguration : TenantEntityConfiguration<ProjectSpace>
{
    public override void Configure(EntityTypeBuilder<ProjectSpace> builder)
    {
        base.Configure(builder);

        builder.ToTable("project_spaces");

        builder.Property(s => s.Id)
            .HasColumnName("id");

        builder.Property(s => s.ProjectWorkspaceId)
            .HasColumnName("project_workspace_id");

        builder.Property(s => s.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.Slug)
            .HasColumnName("slug")
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(s => new { s.ProjectWorkspaceId, s.Slug }).IsUnique();

        builder.Property(s => s.Description)
            .HasColumnName("description");

        builder.Property(s => s.Color)
            .HasColumnName("custom_color")
            .HasMaxLength(16);

        builder.Property(s => s.Icon)
            .HasColumnName("custom_icon")
            .HasMaxLength(64);

        builder.Property(s => s.IsPrivate)
            .HasColumnName("is_private");

        builder.Property(s => s.OrderKey)
            .HasColumnName("order_key")
            .IsRequired();

        builder.Property(s => s.IsArchived)
            .HasColumnName("is_archived");

        builder.Property(w => w.WorkflowId)
            .HasColumnName("workflow_id");

        builder.Property(w => w.StatusId)
            .HasColumnName("status_id");

        builder.Property(s => s.StartDate)
            .HasColumnName("start_date");

        builder.Property(s => s.DueDate)
            .HasColumnName("due_date");

        // Foreign Keys
        builder.HasOne<Workflow>()
            .WithMany()
            .HasForeignKey(w => w.WorkflowId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Status>()
            .WithMany()
            .HasForeignKey(w => w.StatusId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(w => w.WorkflowId);
        builder.HasIndex(w => w.StatusId);
    }
}
