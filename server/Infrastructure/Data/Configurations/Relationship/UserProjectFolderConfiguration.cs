using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Relationship;

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

        builder.HasOne<Domain.Entities.ProjectWorkspace.ProjectFolder>(upf => upf.ProjectFolder)
            .WithMany(pf => pf.Members)
            .HasForeignKey(upf => upf.ProjectFolderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure common Entity properties
        builder.Property(e => e.Version)
            .IsRowVersion();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();
    }
}
