using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship; // For UserProjectFolder

namespace Infrastructure.Data.Configurations.ProjectEntities;

public class ProjectFolderConfiguration : IEntityTypeConfiguration<ProjectFolder>
{
    public void Configure(EntityTypeBuilder<ProjectFolder> builder)
    {
        builder.ToTable("ProjectFolders");

        builder.HasKey(pf => pf.Id);

        builder.Property(pf => pf.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(pf => pf.ProjectWorkspaceId)
            .IsRequired();

        builder.Property(pf => pf.ProjectSpaceId)
            .IsRequired();

        builder.Property(pf => pf.CreatorId)
            .IsRequired();

        builder.Property(pf => pf.IsPrivate)
            .IsRequired();

        builder.Property(pf => pf.IsArchived)
            .IsRequired();

        // Configure relationships
        builder.HasMany(pf => pf.Members)
            .WithOne()
            .HasForeignKey(upf => upf.ProjectFolderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade); // Assuming members are deleted with folder

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
