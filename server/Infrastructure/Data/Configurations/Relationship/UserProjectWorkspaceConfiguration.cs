using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Relationship;
using Domain.Enums; // For Role

namespace Infrastructure.Data.Configurations.Relationship;

public class UserProjectWorkspaceConfiguration : IEntityTypeConfiguration<UserProjectWorkspace>
{
    public void Configure(EntityTypeBuilder<UserProjectWorkspace> builder)
    {
        builder.ToTable("UserProjectWorkspaces");

        // Composite primary key
        builder.HasKey(upw => new { upw.UserId, upw.ProjectWorkspaceId });

        builder.Property(upw => upw.Role)
            .IsRequired()
            .HasConversion<string>(); // Store enum as string

        // Configure relationships
        // Assuming User and ProjectWorkspace entities exist and have Id as primary key
        builder.HasOne<Domain.Entities.User>(upw => upw.User) // Assuming User is a navigation property
            .WithMany() // Assuming User has many UserProjectWorkspaces
            .HasForeignKey(upw => upw.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Domain.Entities.ProjectWorkspace.ProjectWorkspace>(upw => upw.ProjectWorkspace) // Assuming ProjectWorkspace is a navigation property
            .WithMany(pw => pw.Members) // Assuming ProjectWorkspace has a Members collection
            .HasForeignKey(upw => upw.ProjectWorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure common Entity properties (inherited from Entity, but not Aggregate)
        // UserProjectWorkspace is an Entity, not an Aggregate, so it doesn't have DomainEvents
        builder.Property(e => e.Version)
            .IsRowVersion();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();
    }
}
