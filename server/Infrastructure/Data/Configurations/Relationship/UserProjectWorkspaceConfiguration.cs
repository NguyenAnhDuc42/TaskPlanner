using System;
using Domain.Entities.Relationship;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Relationship;

public class UserProjectWorkspaceConfiguration : IEntityTypeConfiguration<UserProjectWorkspace>
{
    public void Configure(EntityTypeBuilder<UserProjectWorkspace> builder)
    {
        builder.ToTable("UserProjectWorkspaces");

        builder.HasKey(x => new { x.UserId, x.ProjectWorkspaceId });

        builder.HasOne(x => x.User)
               .WithMany()
               .HasForeignKey(x => x.UserId);

        builder.HasOne(x => x.ProjectWorkspace)
               .WithMany()
               .HasForeignKey(x => x.ProjectWorkspaceId);
        builder.Property(x => x.IsPending);

        builder.Property(x => x.CreatedAt);

        builder.Property(x => x.Role)
               .HasConversion<string>() 
               .IsRequired();
    }
}