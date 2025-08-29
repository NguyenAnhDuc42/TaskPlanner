using Domain.Entities.ProjectEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.ProjectEntities;

public class ProjectWorkspaceConfiguration : IEntityTypeConfiguration<ProjectWorkspace>
{
    public void Configure(EntityTypeBuilder<ProjectWorkspace> builder)
    {
        builder.ToTable("ProjectWorkspaces");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(w => w.Description)
            .HasMaxLength(1000);

        builder.Property(w => w.JoinCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasIndex(w => w.JoinCode)
            .IsUnique();

        builder.Property(w => w.Color)
            .IsRequired()
            .HasMaxLength(7);

        builder.Property(w => w.Icon)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(w => w.Visibility)
            .IsRequired();

        builder.Property(w => w.IsArchived)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(w => w.CreatorId)
            .IsRequired();

        // Relationships
        builder.HasMany(w => w.Spaces)
            .WithOne()
            .HasForeignKey(s => s.ProjectWorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.Members)
            .WithOne(m => m.ProjectWorkspace)
            .HasForeignKey(m => m.ProjectWorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.Statuses)
            .WithOne()
            .HasForeignKey(s => s.ProjectWorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(w => w.Tags)
            .WithOne()
            .HasForeignKey(s => s.ProjectWorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Common properties from Aggregate
        builder.Property(e => e.Version)
            .IsRowVersion();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        // Ignore DomainEvents
        builder.Ignore(w => w.DomainEvents);
    }
}
