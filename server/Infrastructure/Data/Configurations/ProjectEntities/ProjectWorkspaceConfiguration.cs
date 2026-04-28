using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;
using Domain.Common;

namespace Infrastructure.Data.Configurations;

public class ProjectWorkspaceConfiguration : EntityConfiguration<ProjectWorkspace>
{
    public override void Configure(EntityTypeBuilder<ProjectWorkspace> builder)
    {
        base.Configure(builder);

        builder.ToTable("project_workspaces");

        builder.Property(w => w.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(w => w.Slug)
            .HasColumnName("slug")
            .IsRequired()
            .HasMaxLength(100);
        
        builder.HasIndex(w => w.Slug).IsUnique();

        builder.Property(w => w.Description)
            .HasColumnName("description");

        builder.Property(w => w.JoinCode)
            .HasColumnName("join_code")
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(w => w.Color)
            .HasColumnName("custom_color")
            .HasMaxLength(16);

        builder.Property(w => w.Icon)
            .HasColumnName("custom_icon")
            .HasMaxLength(64);

        builder.Property(w => w.StrictJoin)
            .HasColumnName("strict_join");

        builder.Property(w => w.IsArchived)
            .HasColumnName("is_archived");

        builder.Property(w => w.IsInitialized)
            .HasColumnName("is_initialized");

        builder.HasMany(w => w.Members)
            .WithOne()
            .HasForeignKey(m => m.ProjectWorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
