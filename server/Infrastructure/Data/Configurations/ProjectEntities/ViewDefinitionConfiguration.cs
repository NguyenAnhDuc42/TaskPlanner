using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;
using Domain.Common;

namespace Infrastructure.Data.Configurations;

public class ViewDefinitionConfiguration : TenantEntityConfiguration<ViewDefinition>
{
    public override void Configure(EntityTypeBuilder<ViewDefinition> builder)
    {
        base.Configure(builder);

        builder.ToTable("view_definitions");

        builder.Property(x => x.ProjectSpaceId)
            .HasColumnName("project_space_id");

        builder.Property(x => x.ProjectFolderId)
            .HasColumnName("project_folder_id");

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ViewType)
            .HasColumnName("view_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.IsDefault)
            .HasColumnName("is_default")
            .IsRequired();

        builder.Property(x => x.OrderKey)
            .HasColumnName("order_key")
            .IsRequired();
        
        builder.Property(x => x.FilterConfig)
            .HasColumnName("filter_config_json")
            .HasColumnType("jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null!),
                v => System.Text.Json.JsonSerializer.Deserialize<ViewFilterConfig>(v, (System.Text.Json.JsonSerializerOptions)null!));

        builder.Property(x => x.DisplayConfigJson)
            .HasColumnName("display_config_json")
            .HasColumnType("jsonb");

        builder.HasIndex(x => x.ProjectSpaceId);
        builder.HasIndex(x => x.ProjectFolderId);
    }
}
