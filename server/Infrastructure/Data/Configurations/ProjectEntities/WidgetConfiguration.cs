using Domain.Entities.ProjectEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Infrastructure.Data.Configurations.ProjectEntities;

public class WidgetConfiguration : EntityConfiguration<Widget>
{
    public override void Configure(EntityTypeBuilder<Widget> builder)
    {
        base.Configure(builder);

        builder.ToTable("widgets");

        // core props
        builder.Property(x => x.LayerType).HasColumnName("layer_type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.LayerId).HasColumnName("layer_id").IsRequired();

        // enums as strings
        builder.Property(x => x.Visibility).HasColumnName("visibility").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.WidgetType).HasColumnName("widget_type").HasConversion<string>().HasMaxLength(100).IsRequired();

        // config JSON
        builder.Property(x => x.ConfigJson).HasColumnName("config_json").IsRequired();

        // indexes for common lookups
        builder.HasIndex(x => x.LayerType);
        builder.HasIndex(x => x.LayerId);
        builder.HasIndex(x => new { x.LayerType, x.LayerId });

        builder.HasIndex(x => new { x.LayerType, x.LayerId, x.WidgetType }).IsUnique(false);

        builder.OwnsOne(x => x.Layout, cb =>
        {
            cb.Property(p => p.Col).HasColumnName("layout_col").IsRequired();
            cb.Property(p => p.Row).HasColumnName("layout_row").IsRequired();
            cb.Property(p => p.Width).HasColumnName("layout_width").IsRequired();
            cb.Property(p => p.Height).HasColumnName("layout_height").IsRequired();
        });
    }
}
