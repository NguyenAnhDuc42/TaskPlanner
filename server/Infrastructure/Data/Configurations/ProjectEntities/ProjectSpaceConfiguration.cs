using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship; // For UserProjectSpace
using Domain.Enums; // For SpaceFeatures

namespace Infrastructure.Data.Configurations.ProjectEntities;

public class ProjectSpaceConfiguration : IEntityTypeConfiguration<ProjectSpace>
{
    public void Configure(EntityTypeBuilder<ProjectSpace> builder)
    {
        builder.ToTable("ProjectSpaces");

        builder.HasKey(ps => ps.Id);

        builder.Property(ps => ps.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(ps => ps.Icon)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ps => ps.Color)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ps => ps.ProjectWorkspaceId)
            .IsRequired();

        builder.Property(ps => ps.CreatorId)
            .IsRequired();

        builder.Property(ps => ps.IsPrivate)
            .IsRequired();

        builder.Property(ps => ps.IsPublic)
            .IsRequired();

        builder.Property(ps => ps.IsArchived)
            .IsRequired();

        builder.Property(ps => ps.EnabledFeatures)
            .IsRequired()
            .HasConversion<string>(); // Store enum as string

        // Configure relationships
        builder.HasMany(ps => ps.Members)
            .WithOne()
            .HasForeignKey(ups => ups.ProjectSpaceId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade); // Assuming members are deleted with space

        // Configure common Entity properties
        builder.Property(e => e.Version)
            .IsRowVersion();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        // Ignore domain events collection as it's not persisted
        builder.Ignore(e => e.DomainEvents);
    }
}
