using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.ProjectEntities;

namespace Infrastructure.Data.Configurations.ProjectEntities;

public class ViewDefinitionConfiguration : EntityConfiguration<ViewDefinition>
{
    public override void Configure(EntityTypeBuilder<ViewDefinition> builder)
    {
        base.Configure(builder);

        builder.ToTable("view_definitions");

        builder.Property(x => x.LayerType).HasColumnName("layer_type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.LayerId).HasColumnName("layer_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.ViewType).HasColumnName("view_type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.OrderKey).HasColumnName("order_key").IsRequired();
        builder.Property(x => x.IsDefault).HasColumnName("is_default").IsRequired();
        
        builder.Property(x => x.FilterConfigJson).HasColumnName("filter_config_json");
        builder.Property(x => x.DisplayConfigJson).HasColumnName("display_config_json");

        builder.HasIndex(x => new { x.LayerId, x.LayerType });
        builder.HasIndex(x => new { x.LayerId, x.LayerType, x.OrderKey });
    }
}
