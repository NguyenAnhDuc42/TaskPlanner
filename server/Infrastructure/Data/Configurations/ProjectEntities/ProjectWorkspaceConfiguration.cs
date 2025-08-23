using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Relationship; // For UserProjectWorkspace
using Domain.Entities.Support;
using Domain.Entities.ProjectEntities; // For Status

namespace Infrastructure.Data.Configurations.ProjectEntities;

public class ProjectWorkspaceConfiguration : IEntityTypeConfiguration<ProjectWorkspace>
{
    public void Configure(EntityTypeBuilder<ProjectWorkspace> builder)
    {
        builder.ToTable("ProjectWorkspaces");

        builder.HasKey(pw => pw.Id);

        builder.Property(pw => pw.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(pw => pw.Description)
            .HasMaxLength(1024);

        builder.Property(pw => pw.JoinCode)
            .IsRequired()
            .HasMaxLength(10)
            .IsUnicode(false);

        builder.Property(pw => pw.Color)
            .HasMaxLength(50);

        builder.Property(pw => pw.Icon)
            .HasMaxLength(50);

        builder.Property(pw => pw.CreatorId)
            .IsRequired();

        builder.Property(pw => pw.IsPrivate)
            .IsRequired();

        builder.Property(pw => pw.IsArchived)
            .IsRequired();

        // Configure relationships
        builder.HasMany(pw => pw.Members)
            .WithOne()
            .HasForeignKey(upw => upw.ProjectWorkspaceId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade); // Assuming members are deleted with workspace

        builder.HasMany(pw => pw.DefaultStatuses)
            .WithOne()
            .HasForeignKey(s => s.ProjectWorkspaceId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade); // Assuming statuses are deleted with workspace

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
