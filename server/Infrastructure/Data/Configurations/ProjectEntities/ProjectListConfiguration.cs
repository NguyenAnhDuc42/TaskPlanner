using Domain.Entities.ProjectEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.ProjectEntities;

public class ProjectListConfiguration : IEntityTypeConfiguration<ProjectList>
{
    public void Configure(EntityTypeBuilder<ProjectList> builder)
    {
        builder.ToTable("ProjectLists");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.Description)
            .HasMaxLength(500);

        builder.Property(l => l.Visibility)
            .IsRequired();
            
        builder.Property(l => l.CreatorId)
            .IsRequired();
            
        builder.Property(l => l.IsArchived)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(l => l.StartDate); // Added
        builder.Property(l => l.DueDate); // Added
        builder.Property(l => l.OrderIndex); // Added

        // Relationships
        builder.HasOne<ProjectSpace>()
            .WithMany(s => s.Lists)
            .HasForeignKey(l => l.ProjectSpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        // A list can optionally belong to a folder. The relationship from Folder to List
        // is already configured with DeleteBehavior.Restrict.
        builder.HasOne<ProjectFolder>()
            .WithMany(f => f.Lists)
            .HasForeignKey(l => l.ProjectFolderId)
            .IsRequired(false) // This makes the FK nullable
            .OnDelete(DeleteBehavior.ClientSetNull); // If folder is deleted, set FK to null

        builder.HasMany(l => l.Tasks)
            .WithOne()
            .HasForeignKey(t => t.ProjectListId)
            .OnDelete(DeleteBehavior.Cascade); // Tasks are deleted with their list

        builder.HasMany(l => l.Members)
            .WithOne(m => m.ProjectList)
            .HasForeignKey(m => m.ProjectListId)
            .OnDelete(DeleteBehavior.Cascade);

      

        // Ignore DomainEvents
        builder.Ignore(l => l.DomainEvents);
    }
}