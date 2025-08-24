using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Relationship;
using Domain.Entities.ProjectEntities; // Add this using statement for ProjectFolder

namespace Infrastructure.Data.Configurations.Relationship;

public class UserProjectFolderConfiguration : IEntityTypeConfiguration<UserProjectFolder>
{
    public void Configure(EntityTypeBuilder<UserProjectFolder> builder)
    {
        builder.ToTable("UserProjectFolders");

        // Composite primary key
        builder.HasKey(upf => new { upf.UserId, upf.ProjectFolderId });

        // Configure relationships
        builder.HasOne<Domain.Entities.User>(upf => upf.User)
            .WithMany()
            .HasForeignKey(upf => upf.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ProjectFolder>(upf => upf.ProjectFolder) // Corrected namespace
            .WithMany(pf => pf.Members)
            .HasForeignKey(upf => upf.ProjectFolderId)
            .OnDelete(DeleteBehavior.Cascade);

    // UserProjectFolder is a relationship POCO and does not have Version/CreatedAt/UpdatedAt
    }
}
