using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using src.Domain.Entities.WorkspaceEntity;
using src.Domain.Entities.WorkspaceEntity.SupportEntiy;

namespace src.Infrastructure.Data.Configurations;

public class StatusConfigurations : IEntityTypeConfiguration<Status>
{
    public void Configure(EntityTypeBuilder<Status> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Color).HasMaxLength(100);
        builder.Property(s => s.Type).IsRequired();
        builder.Property(s => s.SpaceId).IsRequired();

        builder.HasIndex(s => s.SpaceId);


        builder.HasMany(s => s.Tasks).WithOne().HasForeignKey(t => t.StatusId);

    }
}
