using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using src.Domain.Entities.WorkspaceEntity.Relationships;

namespace src.Infrastructure.Data.Configurations.Relationships;

public class  UserWorkspaceConfigurations : IEntityTypeConfiguration<UserWorkspace>
{
    public void Configure(EntityTypeBuilder<UserWorkspace> builder)
    {
        builder.ToTable("UserWorkspaces");
        builder.HasKey(uw => new { uw.UserId, uw.WorkspaceId });
        builder.HasOne(uw => uw.User)
            .WithMany(u => u.Workspaces)
            .HasForeignKey(uw => uw.UserId);
        builder.HasOne(uw => uw.Workspace)
            .WithMany(w => w.Members)
            .HasForeignKey(uw => uw.WorkspaceId);

        builder.Property(uw => uw.Role)
                .HasColumnName("Role")
                .IsRequired();

        builder.HasIndex(uw => uw.UserId);
        builder.HasIndex(uw => uw.WorkspaceId);
    }
}

