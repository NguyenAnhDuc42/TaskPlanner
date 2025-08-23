using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship; // For UserProjectList

namespace Infrastructure.Data.Configurations.ProjectEntities;

public class ProjectListConfiguration : IEntityTypeConfiguration<ProjectList>
{
    public void Configure(EntityTypeBuilder<ProjectList> builder)
    {
        builder.ToTable("ProjectLists");

        builder.HasKey(pl => pl.Id);

        builder.Property(pl => pl.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(pl => pl.ProjectWorkspaceId)
            .IsRequired();

        builder.Property(pl => pl.ProjectSpaceId)
            .IsRequired();

        // ProjectFolderId is nullable
        builder.Property(pl => pl.ProjectFolderId)
            .IsRequired(false);

        builder.Property(pl => pl.CreatorId)
            .IsRequired();

        // DefaultAssigneeId is nullable
        builder.Property(pl => pl.DefaultAssigneeId)
            .IsRequired(false);

        builder.Property(pl => pl.IsPrivate)
            .IsRequired();

        builder.Property(pl => pl.IsArchived)
            .IsRequired();

        builder.Property(pl => pl.OrderIndex)
            .IsRequired();

        // StartDate and DueDate are nullable
        builder.Property(pl => pl.StartDate)
            .IsRequired(false);

        builder.Property(pl => pl.DueDate)
            .IsRequired(false);

        // Configure relationships
        // TaskIds is a collection of Guids, typically stored as a JSON column or a separate join table
        // For simplicity, let's assume it's stored as a JSON string or similar if EF Core supports it directly.
        // If not, it would require a separate entity for TaskList (ListId, TaskId)
        // For now, I'll ignore it as a direct collection of Guids is not directly mapped by EF Core.
        // A common pattern is to have a navigation property to ProjectTask and let EF manage the relationship.
        // Given it's a List<Guid>, it implies a many-to-many or one-to-many where ProjectList "owns" task IDs.
        // If ProjectTask is an aggregate root, ProjectList should only store its ID.
        // This might need a custom value converter or a join entity if persisted directly.
        // For now, I'll ignore it as it's a private List<Guid> and not a navigation property.
        builder.Ignore(pl => pl.TaskIds);


        builder.HasMany(pl => pl.Members)
            .WithOne()
            .HasForeignKey(upl => upl.ProjectListId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade); // Assuming members are deleted with list

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
