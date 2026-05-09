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

        builder.Property(s => s.DefaultDocumentId)
            .HasColumnName("default_document_id")
            .IsRequired();

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

    }
}
