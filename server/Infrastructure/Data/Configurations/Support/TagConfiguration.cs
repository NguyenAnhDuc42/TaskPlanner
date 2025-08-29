using System;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Support;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(x => x.Color)
               .HasMaxLength(50);

        builder.HasOne<ProjectWorkspace>()
               .WithMany(w => w.Tags)
               .HasForeignKey(x => x.ProjectWorkspaceId);
    }
}
