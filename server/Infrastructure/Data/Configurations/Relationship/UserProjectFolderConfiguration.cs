using System;
using Domain.Entities.Relationship;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Relationship;

public class UserProjectFolderConfiguration : IEntityTypeConfiguration<UserProjectFolder>
{
    public void Configure(EntityTypeBuilder<UserProjectFolder> builder)
    {
        builder.ToTable("UserProjectFolders");

        builder.HasKey(x => new { x.UserId, x.ProjectFolderId });

        builder.HasOne(x => x.User)
               .WithMany()
               .HasForeignKey(x => x.UserId);

        builder.HasOne(x => x.ProjectFolder)
               .WithMany()
               .HasForeignKey(x => x.ProjectFolderId);

        builder.Property(x => x.CreatedAt);
    }
}
