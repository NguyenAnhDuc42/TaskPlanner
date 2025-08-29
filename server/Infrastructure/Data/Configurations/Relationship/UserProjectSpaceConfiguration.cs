using System;
using Domain.Entities.Relationship;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Relationship;

public class UserProjectSpaceConfiguration : IEntityTypeConfiguration<UserProjectSpace>
{
    public void Configure(EntityTypeBuilder<UserProjectSpace> builder)
    {
        builder.ToTable("UserProjectSpaces");

        builder.HasKey(x => new { x.UserId, x.ProjectSpaceId });

        builder.HasOne(x => x.User)
               .WithMany()
               .HasForeignKey(x => x.UserId);

        builder.HasOne(x => x.ProjectSpace)
               .WithMany()
               .HasForeignKey(x => x.ProjectSpaceId);

        builder.Property(x => x.CreatedAt);
    }
}