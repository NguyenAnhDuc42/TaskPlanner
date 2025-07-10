using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using src.Domain.Entities.WorkspaceEntity;

namespace src.Infrastructure.Data.Configurations;

public class SpaceConfigurations : IEntityTypeConfiguration<Space>
{
    public void Configure(EntityTypeBuilder<Space> builder)
    {
        builder.ToTable("Spaces");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).IsRequired();
        builder.Property(s => s.WorkSpaceId).IsRequired();
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(s => s.Icon);
        builder.Property(s => s.IsPrivate);
        builder.Property(s => s.IsArchived);
        builder.Property(s => s.CreatorId)
            .IsRequired();
        builder.Property(s => s.CreatorName);

        //index
        builder.HasIndex(s => s.Name);
        builder.HasIndex(s => s.WorkSpaceId);
        builder.HasIndex(s => s.CreatorId);

        //relationships
        builder.HasMany(s => s.Lists)
            .WithOne()
            .HasForeignKey(l => l.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(s => s.Folders)
            .WithOne()
            .HasForeignKey(f => f.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(s => s.Members)
            .WithOne(us => us.Space)
            .HasForeignKey(us => us.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);


        //ignore
        builder.Ignore(u => u.DomainEvents);
    }
}
