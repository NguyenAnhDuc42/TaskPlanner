using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.ProjectEntities;

namespace Infrastructure.Data.Configurations.ProjectEntities;

public class ProjectFolderConfiguration : EntityConfiguration<ProjectFolder>
{
    public override void Configure(EntityTypeBuilder<ProjectFolder> builder)
    {
        base.Configure(builder);

        builder.ToTable("project_folders");

        builder.Property(x => x.ProjectSpaceId).HasColumnName("project_space_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.OrderKey).HasColumnName("order_key");
        builder.Property(x => x.IsPrivate).HasColumnName("is_private").IsRequired();
        builder.Property(x => x.IsArchived).HasColumnName("is_archived").IsRequired();
        builder.Property(x => x.CreatorId).HasColumnName("creator_id").IsRequired();
        builder.Property(x => x.NextListOrder).HasColumnName("next_list_order").IsRequired();


        builder.OwnsOne(x => x.Customization, cb =>
        {
            cb.Property(p => p.Color).HasColumnName("custom_color").HasMaxLength(32).IsRequired();
            cb.Property(p => p.Icon).HasColumnName("custom_icon").HasMaxLength(128).IsRequired();
        });

        builder.HasIndex(x => x.ProjectSpaceId);
        builder.HasIndex(x => new { x.ProjectSpaceId, x.OrderKey });
    }
}
