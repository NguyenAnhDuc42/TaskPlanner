using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Relationship;
using Domain.Enums; // For Role
using Domain.Entities.ProjectEntities; // Add this using statement for ProjectWorkspace

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

        builder.HasOne<ProjectWorkspace>(upw => upw.ProjectWorkspace) // Corrected namespace
            .WithMany(pw => pw.Members) // Assuming ProjectWorkspace has a Members collection
            .HasForeignKey(upw => upw.ProjectWorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

    // UserProjectWorkspace is a relationship entity (POCO) and does not expose Version/CreatedAt/UpdatedAt
    }
}
