using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Support.Widget;

namespace Infrastructure.Data.Configurations.Support;

public class UserWidgetConfiguration : EntityConfiguration<UserWidget>
{
    public override void Configure(EntityTypeBuilder<UserWidget> builder)
    {
        base.Configure(builder);

        builder.ToTable("user_widgets");

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.WidgetId).IsRequired();
        builder.Property(x => x.PositionIndex).IsRequired();
        builder.Property(x => x.Visible).IsRequired();
        builder.Property(x => x.ConfigOverrideJson).HasColumnName("config_override_json");

        // Owned value object: WidgetLayout
        builder.OwnsOne(x => x.Layout, lb =>
        {
            lb.Property(p => p.Col).HasColumnName("layout_col").IsRequired();
            lb.Property(p => p.Row).HasColumnName("layout_row").IsRequired();
            lb.Property(p => p.Width).HasColumnName("layout_width").IsRequired();
            lb.Property(p => p.Height).HasColumnName("layout_height").IsRequired();

            // If you want to index layout positions, add indexes here:
             lb.HasIndex(p => new { p.Col, p.Row });
        });

        // indexes
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.WidgetId);
        builder.HasIndex(x => new { x.UserId, x.PositionIndex });
    }
}
