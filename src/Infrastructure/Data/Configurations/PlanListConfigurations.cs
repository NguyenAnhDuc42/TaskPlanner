using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using src.Domain.Entities.WorkspaceEntity;

namespace src.Infrastructure.Data.Configurations;

public class PlanListConfigurations : IEntityTypeConfiguration<PlanList>
{
    public void Configure(EntityTypeBuilder<PlanList> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.WorkspaceId).IsRequired();
        builder.Property(l => l.SpaceId).IsRequired();
        builder.Property(l => l.FolderId).IsRequired(false);
        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(l => l.IsPrivate);
        builder.Property(l => l.IsArchived);
        builder.Property(l => l.OrderIndex);
        builder.Property(l => l.CreatorId)
            .IsRequired();

        builder.HasIndex(l => l.WorkspaceId);
        builder.HasIndex(l => l.SpaceId);
        builder.HasIndex(l => l.FolderId);
        builder.HasIndex(l => l.CreatorId);


        builder.HasMany(l => l.Tasks)
            .WithOne()
            .HasForeignKey(t => t.ListId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(l => l.Members)
            .WithOne()
            .HasForeignKey(ul => ul.ListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(l => l.DomainEvents);
    }
}