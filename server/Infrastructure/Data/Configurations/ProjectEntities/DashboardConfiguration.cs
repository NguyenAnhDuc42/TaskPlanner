using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Data.Configurations;

public class DashboardConfiguration : EntityConfiguration<Dashboard>
{
    public override void Configure(EntityTypeBuilder<Dashboard> builder)
    {
        base.Configure(builder);

        builder.ToTable("dashboards");

        builder.Property(x => x.ProjectWorkspaceId).HasColumnName("project_workspace_id").IsRequired();
        builder.HasIndex(x => x.ProjectWorkspaceId);

        builder.Property(x => x.LayerType).HasColumnName("layer_type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.LayerId).HasColumnName("layer_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsShared).HasColumnName("is_shared").IsRequired();
        builder.Property(x => x.IsMain).HasColumnName("is_main").IsRequired();

        builder.HasMany(x => x.Widgets)
            .WithOne()
            .HasForeignKey(w => w.DashboardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Dashboard.Widgets))?.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.LayerId);
        builder.HasIndex(x => new { x.LayerType, x.LayerId });
    }
}
