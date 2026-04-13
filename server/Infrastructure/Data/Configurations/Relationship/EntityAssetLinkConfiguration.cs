using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Data.Configurations.Relationship;

public class EntityAssetLinkConfiguration : EntityConfiguration<EntityAssetLink>
{
    public override void Configure(EntityTypeBuilder<EntityAssetLink> builder)
    {
        base.Configure(builder);

        builder.ToTable("entity_asset_links");

        // Composite PK to avoid duplicate links of the same type to the same parent
        builder.HasKey(x => new { x.AssetId, x.AssetType, x.ParentEntityType, x.ParentEntityId });

        builder.Property(x => x.AssetId).HasColumnName("asset_id").IsRequired();
        builder.Property(x => x.AssetType).HasColumnName("asset_type").HasConversion<string>().HasMaxLength(100).IsRequired();
        builder.Property(x => x.ParentEntityId).HasColumnName("parent_entity_id").IsRequired();
        builder.Property(x => x.ParentEntityType).HasColumnName("parent_entity_type").HasConversion<string>().HasMaxLength(100).IsRequired();

        // High-Performance Indexes for Hierarchy lookups
        builder.HasIndex(x => new { x.ParentEntityType, x.ParentEntityId, x.AssetType });
        builder.HasIndex(x => new { x.AssetId, x.AssetType });
    }
}
