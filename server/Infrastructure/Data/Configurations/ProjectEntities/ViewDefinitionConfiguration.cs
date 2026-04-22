using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Data.Configurations;

public class ViewDefinitionConfiguration : EntityConfiguration<ViewDefinition>
{
    public override void Configure(EntityTypeBuilder<ViewDefinition> builder)
    {
        base.Configure(builder);

        builder.ToTable("view_definitions");

        builder.Property(x => x.ProjectWorkspaceId).HasColumnName("project_workspace_id").IsRequired();
        builder.HasIndex(x => x.ProjectWorkspaceId);

        builder.Property(x => x.LayerType).HasColumnName("layer_type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.LayerId).HasColumnName("layer_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.ViewType).HasColumnName("view_type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.IsDefault).HasColumnName("is_default").IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired();
        
        builder.Property(x => x.FilterConfig)
            .HasColumnName("filter_config_json")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null!),
                v => System.Text.Json.JsonSerializer.Deserialize<ViewFilterConfig>(v, (System.Text.Json.JsonSerializerOptions)null!) ?? ViewFilterConfig.CreateDefault());

        builder.Property(x => x.DisplayConfigJson).HasColumnName("display_config_json");

        builder.HasIndex(x => new { x.LayerId, x.LayerType });
    }
}
