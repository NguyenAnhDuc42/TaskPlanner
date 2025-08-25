using Domain.Entities.ProjectEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.ProjectEntities;

public class ProjectSpaceConfiguration : IEntityTypeConfiguration<ProjectSpace>
{
    public void Configure(EntityTypeBuilder<ProjectSpace> builder)
    {
        builder.ToTable("ProjectSpaces");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100); // Corrected from 200

        builder.Property(s => s.Description)
            .HasMaxLength(500); // Corrected from 1000

        builder.Property(s => s.Icon)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.Color)
            .IsRequired()
            .HasMaxLength(7);

        builder.Property(s => s.Visibility)
            .IsRequired();

        builder.Property(s => s.IsArchived) // Added
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.OrderIndex); // Added

        builder.Property(s => s.CreatorId)
            .IsRequired();

        // Relationships
        builder.HasOne<ProjectWorkspace>() // Define the relationship to the parent workspace
            .WithMany(w => w.Spaces)
            .HasForeignKey(s => s.ProjectWorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Folders)
            .WithOne() // A folder belongs to one space
            .HasForeignKey(f => f.ProjectSpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Lists)
            .WithOne() // A list belongs to one space
            .HasForeignKey(l => l.ProjectSpaceId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(s => s.Members)
            .WithOne(m => m.ProjectSpace)
            .HasForeignKey(m => m.ProjectSpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Common properties from Aggregate
        builder.Property(e => e.Version)
            .IsRowVersion();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        // Ignore DomainEvents
        builder.Ignore(s => s.DomainEvents);
    }
}