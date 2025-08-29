using System;
using Domain.Entities;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Support;

public class TimeLogConfiguration : IEntityTypeConfiguration<TimeLog>
{
    public void Configure(EntityTypeBuilder<TimeLog> builder)
    {
        builder.ToTable("TimeLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Duration)
               .IsRequired();

        builder.Property(x => x.Description)
               .HasMaxLength(500);

        builder.Property(x => x.LoggedAt)
               .IsRequired();

        builder.HasOne<ProjectTask>()
               .WithMany(t => t.TimeLogs)
               .HasForeignKey(x => x.ProjectTaskId);

        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(x => x.UserId);
    }
}