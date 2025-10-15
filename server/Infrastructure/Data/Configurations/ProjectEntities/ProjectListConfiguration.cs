using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.ProjectEntities;

namespace Infrastructure.Data.Configurations.ProjectEntities;

public class ProjectListConfiguration : EntityConfiguration<ProjectList>
{
    public override void Configure(EntityTypeBuilder<ProjectList> builder)
    {
        base.Configure(builder);

        builder.ToTable("project_lists");

        builder.Property(x => x.ProjectSpaceId).IsRequired();
        builder.Property(x => x.ProjectFolderId);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.OrderKey);
        builder.Property(x => x.IsPrivate).IsRequired();
        builder.Property(x => x.IsArchived).IsRequired();
        builder.Property(x => x.CreatorId).IsRequired();
        builder.Property(x => x.StartDate);
        builder.Property(x => x.DueDate);

        builder.OwnsOne(x => x.Customization, cb =>
        {
            cb.Property(p => p.Color).HasColumnName("custom_color").HasMaxLength(32).IsRequired();
            cb.Property(p => p.Icon).HasColumnName("custom_icon").HasMaxLength(128).IsRequired();
        });

        builder.HasIndex(x => x.ProjectSpaceId);
        builder.HasIndex(x => new { x.ProjectSpaceId, x.ProjectFolderId });
    }
}
