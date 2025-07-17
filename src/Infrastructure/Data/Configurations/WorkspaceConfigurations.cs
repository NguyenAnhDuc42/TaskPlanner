using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using src.Domain.Entities.WorkspaceEntity;

namespace src.Infrastructure.Data.Configurations;

public class WorkspaceConfigurations : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("Workspaces");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(w => w.Description)
            .IsRequired(false)
            .HasMaxLength(1000);
        builder.Property(w => w.Color);
        builder.Property(w => w.IsPrivate);
        builder.Property(w => w.CreatorId)
            .IsRequired();

        //index
        builder.HasIndex(w => w.Name);
        builder.HasIndex(w => w.CreatorId);

        //relationships
        builder.HasMany(w => w.Spaces)
            .WithOne().HasForeignKey(s => s.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(w => w.Members)
            .WithOne()
            .HasForeignKey(uw => uw.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);


        //ignore
        builder.Ignore(u => u.DomainEvents);
    }
}
