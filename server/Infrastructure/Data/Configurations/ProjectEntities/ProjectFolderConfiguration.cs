using Domain.Entities.ProjectEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.ProjectEntities;

public class ProjectFolderConfiguration : EntityConfiguration<ProjectFolder>
{
    public override void Configure(EntityTypeBuilder<ProjectFolder> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.ProjectSpaceId).IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Color).HasMaxLength(20);
        builder.Property(x => x.CreatorId).IsRequired();

        builder.HasIndex(x => new { x.ProjectSpaceId, x.Name });
    }
}