using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using src.Domain.Entities.UserEntity;
using src.Domain.Entities.WorkspaceEntity;
using src.Domain.Entities.WorkspaceEntity.Relationships;

namespace src.Infrastructure.Data.Configurations.Relationships;

public class UserListConfigurations : IEntityTypeConfiguration<UserList>
{
    public void Configure(EntityTypeBuilder<UserList> builder)
    {
        builder.ToTable("UserList");
        builder.HasKey(uw => new { uw.UserId, uw.ListId });
        builder.HasOne<User>()
            .WithMany(u => u.Lists)
            .HasForeignKey(uw => uw.UserId);
        builder.HasOne<PlanList>()
            .WithMany(w => w.Members)
            .HasForeignKey(uw => uw.ListId);

        builder.HasIndex(uw => uw.UserId);
        builder.HasIndex(uw => uw.ListId);
    }
}
