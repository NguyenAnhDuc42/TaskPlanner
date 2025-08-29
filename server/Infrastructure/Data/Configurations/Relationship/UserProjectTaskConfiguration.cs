using System;
using Domain.Entities.Relationship;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Relationship;

public class UserProjectTaskConfiguration : IEntityTypeConfiguration<UserProjectTask>
{
    public void Configure(EntityTypeBuilder<UserProjectTask> builder)
    {
        builder.ToTable("UserProjectTasks");

        builder.HasKey(x => new { x.UserId, x.ProjectTaskId });

        builder.HasOne(x => x.User)
               .WithMany()
               .HasForeignKey(x => x.UserId);

        builder.HasOne(x => x.ProjectTask)
               .WithMany()
               .HasForeignKey(x => x.ProjectTaskId);

        builder.Property(x => x.CreatedAt);
    }
}