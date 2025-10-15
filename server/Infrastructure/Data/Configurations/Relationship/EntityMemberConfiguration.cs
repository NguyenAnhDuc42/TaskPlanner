using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Relationship;

namespace Infrastructure.Data.Configurations.Relationship;

public class EntityMemberConfiguration : CompositeConfiguration<EntityMember>
{
    public override void Configure(EntityTypeBuilder<EntityMember> builder)
    {
        base.Configure(builder);

        builder.ToTable("entity_members");

        // Composite PK: User + Entity + EntityType (prevents duplicate membership rows)
        builder.HasKey(x => new { x.UserId, x.EntityId, x.EntityType });

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.EntityId).IsRequired();
        builder.Property(x => x.EntityType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.AccessLevel).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.NotificationsEnabled).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();

        // Indexes for common queries
        builder.HasIndex(x => x.EntityId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.EntityId, x.AccessLevel });
    }
}
