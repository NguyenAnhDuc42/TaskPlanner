using Domain.Entities.Support.Widget;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Infrastructure.Data.Configurations.Support;

public class WidgetConfiguration : EntityConfiguration<Widget>
{
    public override void Configure(EntityTypeBuilder<Widget> builder)
    {
        base.Configure(builder);

        builder.ToTable("widgets");

        // core props
        builder.Property(x => x.ScopeType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.ScopeId).IsRequired();
        builder.Property(x => x.CreatorId).IsRequired();

        // enums as strings
        builder.Property(x => x.Visibility).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.WidgetType).HasConversion<string>().HasMaxLength(100).IsRequired();

        // config JSON - store as text/jsonb depending on provider; leaving generic as string
        builder.Property(x => x.ConfigJson).HasColumnName("config_json").IsRequired();

        // indexes for common lookups
        builder.HasIndex(x => x.ScopeType);
        builder.HasIndex(x => x.ScopeId);
        builder.HasIndex(x => new { x.ScopeType, x.ScopeId });
        builder.HasIndex(x => x.CreatorId);

        // optional: if you want canonical widget uniqueness per scope+type you can enable:
         builder.HasIndex(x => new { x.ScopeType, x.ScopeId, x.WidgetType }).IsUnique(false);
    }
}
