using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;
using Domain.Common;

namespace Infrastructure.Data.Configurations;

public class ProjectFolderConfiguration : TenantEntityConfiguration<ProjectFolder>
{
    public override void Configure(EntityTypeBuilder<ProjectFolder> builder)
    {
        base.Configure(builder);

        builder.ToTable("project_folders");

        builder.Property(f => f.Id)
            .HasColumnName("id");

        builder.Property(f => f.ProjectWorkspaceId)
            .HasColumnName("project_workspace_id");

        builder.Property(f => f.ProjectSpaceId)
            .HasColumnName("project_space_id");

        builder.Property(f => f.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(f => f.Slug)
            .HasColumnName("slug")
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(f => new { f.ProjectSpaceId, f.Slug }).IsUnique();

        builder.Property(f => f.DefaultDocumentId)
            .HasColumnName("default_document_id")
            .IsRequired();

        builder.Property(f => f.OrderKey)
            .HasColumnName("order_key")
            .IsRequired();

        builder.Property(f => f.IsPrivate)
            .HasColumnName("is_private");

        builder.Property(f => f.IsArchived)
            .HasColumnName("is_archived");

        builder.Property(f => f.StartDate).HasColumnName("start_date");
        builder.Property(f => f.DueDate).HasColumnName("due_date");

        builder.Property(f => f.Color)
            .HasColumnName("custom_color")
            .HasMaxLength(16);

        builder.Property(f => f.Icon)
            .HasColumnName("custom_icon")
            .HasMaxLength(64);



        builder.Property(f => f.StatusId)
            .HasColumnName("status_id");

        // Foreign Keys
        builder.HasOne<ProjectSpace>()
            .WithMany()
            .HasForeignKey(f => f.ProjectSpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Status>()
            .WithMany()
            .HasForeignKey(f => f.StatusId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(f => f.ProjectSpaceId);
        builder.HasIndex(f => f.StatusId);

        builder.HasIndex(f => new { f.ProjectWorkspaceId, f.ProjectSpaceId, f.OrderKey, f.Id })
            .HasFilter("\"deleted_at\" IS NULL AND \"is_archived\" = false");
    }
}
