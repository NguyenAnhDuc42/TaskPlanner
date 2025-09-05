using Domain.Entities.ProjectEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.ProjectEntities;

public class ProjectFolderConfiguration : IEntityTypeConfiguration<ProjectFolder>
{
    public void Configure(EntityTypeBuilder<ProjectFolder> builder)
    {
        builder.ToTable("ProjectFolders");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.Description)
            .HasMaxLength(500);


        builder.Property(f => f.Visibility)
            .IsRequired();
            
        builder.Property(f => f.IsArchived) // Added
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(f => f.OrderKey); // Added

        builder.Property(f => f.CreatorId)
            .IsRequired();

        // Relationships
        builder.HasOne<ProjectSpace>()
            .WithMany()
            .HasForeignKey(f => f.ProjectSpaceId)
            .OnDelete(DeleteBehavior.Cascade); // Folders are deleted with their space

        builder.HasMany<ProjectList>()
            .WithOne() // A list can belong to one folder
            .HasForeignKey(l => l.ProjectFolderId)
            .OnDelete(DeleteBehavior.SetNull); // Corrected from Restrict to SetNull

        builder.HasMany(f => f.Members)
            .WithOne(m => m.ProjectFolder)
            .HasForeignKey(m => m.ProjectFolderId)
            .OnDelete(DeleteBehavior.Cascade);


        // Ignore DomainEvents
        builder.Ignore(f => f.DomainEvents);
    }
}