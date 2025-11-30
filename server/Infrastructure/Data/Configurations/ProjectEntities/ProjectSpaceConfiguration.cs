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

        builder.Property(x => x.ProjectWorkspaceId).HasColumnName("project_workspace_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(2000);
        builder.Property(x => x.IsPrivate).HasColumnName("is_private").IsRequired();
        builder.Property(x => x.IsArchived).HasColumnName("is_archived").IsRequired();
        builder.Property(x => x.InheritStatus).HasColumnName("inherit_status").IsRequired();
        builder.Property(x => x.OrderKey).HasColumnName("order_key");
        builder.Property(x => x.NextItemOrder).HasColumnName("next_item_order").IsRequired();


        builder.OwnsOne(x => x.Customization, cb =>
        {
            cb.Property(p => p.Color).HasColumnName("custom_color").HasMaxLength(32).IsRequired();
            cb.Property(p => p.Icon).HasColumnName("custom_icon").HasMaxLength(128).IsRequired();
        });

        builder.HasIndex(x => x.ProjectWorkspaceId);
        builder.HasIndex(x => new { x.ProjectWorkspaceId, x.IsPrivate });
    }
}
