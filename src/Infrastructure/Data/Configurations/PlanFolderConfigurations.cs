using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using src.Domain.Entities.WorkspaceEntity;

namespace src.Infrastructure.Data.Configurations;

public class PlanFolderConfigurations : IEntityTypeConfiguration<PlanFolder>
{
    public void Configure(EntityTypeBuilder<PlanFolder> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.WorkspaceId).IsRequired();
        builder.Property(f => f.SpaceId).IsRequired();
        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(f => f.IsPrivate);
        builder.Property(f => f.IsArchived);
        builder.Property(f => f.CreatorId).IsRequired();

        builder.HasIndex(f => f.WorkspaceId);
        builder.HasIndex(f => f.SpaceId);
        builder.HasIndex(f => f.CreatorId);

        builder.HasMany(f => f.Lists)
            .WithOne()
            .HasForeignKey(l => l.FolderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(f => f.Members)
            .WithOne()
            .HasForeignKey(uf => uf.FolderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(f => f.DomainEvents);
    }
}
