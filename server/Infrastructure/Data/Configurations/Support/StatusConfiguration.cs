using System;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Support;

public class StatusConfiguration : IEntityTypeConfiguration<Status>
{
    public void Configure(EntityTypeBuilder<Status> builder)
    {
        builder.ToTable("Statuses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(x => x.Color)
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(x => x.OrderIndex)
               .IsRequired();

        builder.Property(x => x.IsDefaultStatus)
               .IsRequired();

        builder.HasOne<ProjectWorkspace>()
               .WithMany(w => w.Statuses)
               .HasForeignKey(x => x.ProjectWorkspaceId);

        builder.HasOne<ProjectSpace>()
               .WithMany(s => s.Statuses)
               .HasForeignKey(x => x.ProjectSpaceId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
