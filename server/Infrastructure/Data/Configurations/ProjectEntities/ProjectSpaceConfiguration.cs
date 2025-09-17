using Domain.Entities.ProjectEntities;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.ProjectEntities;

public class ProjectSpaceConfiguration : EntityConfiguration<ProjectSpace>
{
    public override void Configure(EntityTypeBuilder<ProjectSpace> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.ProjectWorkspaceId).IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Icon).HasMaxLength(50);
        builder.Property(x => x.Color).HasMaxLength(20);

        builder.Property(x => x.CreatorId).IsRequired();

        builder.HasIndex(x => new { x.ProjectWorkspaceId, x.Name });
    }
}