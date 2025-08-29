using System;
using Domain.Entities.Relationship;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Relationship;

public class UserProjectListConfiguration : IEntityTypeConfiguration<UserProjectList>
{
    public void Configure(EntityTypeBuilder<UserProjectList> builder)
    {
        builder.ToTable("UserProjectLists");

        builder.HasKey(x => new { x.UserId, x.ProjectListId });

        builder.HasOne(x => x.User)
               .WithMany()
               .HasForeignKey(x => x.UserId);

        builder.HasOne(x => x.ProjectList)
               .WithMany()
               .HasForeignKey(x => x.ProjectListId);

        builder.Property(x => x.CreatedAt);
    }
}
