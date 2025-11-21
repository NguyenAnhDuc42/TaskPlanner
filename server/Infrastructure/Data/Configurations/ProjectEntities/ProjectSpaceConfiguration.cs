using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.ProjectEntities;

namespace Infrastructure.Data.Configurations.ProjectEntities;

public class ProjectSpaceConfiguration : EntityConfiguration<ProjectSpace>
{
    public override void Configure(EntityTypeBuilder<ProjectSpace> builder)
    {
        base.Configure(builder);

        builder.ToTable("project_spaces");

        builder.Property(x => x.ProjectWorkspaceId).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.IsPrivate).IsRequired();
        builder.Property(x => x.IsArchived).IsRequired();
        builder.Property(x => x.OrderKey);
        builder.Property(x => x.CreatorId).IsRequired();
        builder.Property(x => x.NextEntityOrder).IsRequired();


        builder.OwnsOne(x => x.Customization, cb =>
        {
            cb.Property(p => p.Color).HasColumnName("custom_color").HasMaxLength(32).IsRequired();
            cb.Property(p => p.Icon).HasColumnName("custom_icon").HasMaxLength(128).IsRequired();
        });

        builder.HasIndex(x => x.ProjectWorkspaceId);
        builder.HasIndex(x => new { x.ProjectWorkspaceId, x.IsPrivate });
    }
}
