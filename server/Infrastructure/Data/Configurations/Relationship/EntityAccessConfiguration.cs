using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Relationship;

namespace Infrastructure.Data.Configurations.Relationship;

public class EntityAccessConfiguration : EntityConfiguration<EntityAccess>
{
    public override void Configure(EntityTypeBuilder<EntityAccess> builder)
    {
        base.Configure(builder);

        builder.ToTable("entity_access");

        builder.Property(x => x.WorkspaceMemberId).HasColumnName("workspace_member_id").IsRequired();
        builder.Property(x => x.EntityId).HasColumnName("entity_id").IsRequired();
        builder.Property(x => x.EntityLayer).HasColumnName("entity_layer").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.AccessLevel).HasColumnName("access_level").HasConversion<string>().HasMaxLength(50).IsRequired();

        // Indexes for common queries
        builder.HasIndex(x => x.WorkspaceMemberId);
        builder.HasIndex(x => x.EntityId);
        builder.HasIndex(x => new { x.EntityId, x.EntityLayer });
        builder.HasIndex(x => new { x.WorkspaceMemberId, x.EntityId });
    }
}