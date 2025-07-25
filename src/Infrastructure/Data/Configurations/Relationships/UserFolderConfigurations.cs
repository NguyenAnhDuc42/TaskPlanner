using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using src.Domain.Entities.UserEntity;
using src.Domain.Entities.WorkspaceEntity;
using src.Domain.Entities.WorkspaceEntity.Relationships;

namespace src.Infrastructure.Data.Configurations.Relationships;

public class UserFolderConfigurations : IEntityTypeConfiguration<UserFolder>
{
    public void Configure(EntityTypeBuilder<UserFolder> builder)
    {
        builder.ToTable("UserFolders");
        builder.HasKey(uw => new { uw.UserId, uw.FolderId });
        builder.HasOne<User>()
            .WithMany(u => u.Folders)
            .HasForeignKey(uw => uw.UserId);
        builder.HasOne<PlanFolder>()
            .WithMany(w => w.Members)
            .HasForeignKey(uw => uw.FolderId);

        builder.HasIndex(uw => uw.UserId);
        builder.HasIndex(uw => uw.FolderId);
    }
}
