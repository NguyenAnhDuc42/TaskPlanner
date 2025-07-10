using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using src.Domain.Entities.WorkspaceEntity.Relationships;

namespace src.Infrastructure.Data.Configurations.Relationships;

public class UserSpaceConfigurations : IEntityTypeConfiguration<UserSpace>
{
    public void Configure(EntityTypeBuilder<UserSpace> builder)
    {
        builder.ToTable("UserSpaces");
        builder.HasKey(uw => new { uw.UserId, uw.SpaceId });
        builder.HasOne(uw => uw.User)
           .WithMany(u => u.Spaces)
           .HasForeignKey(uw => uw.UserId);
        builder.HasOne(uw => uw.Space)
            .WithMany(w => w.Members)
            .HasForeignKey(uw => uw.SpaceId);

        builder.HasIndex(uw => uw.UserId);
        builder.HasIndex(uw => uw.SpaceId);
    }
}
