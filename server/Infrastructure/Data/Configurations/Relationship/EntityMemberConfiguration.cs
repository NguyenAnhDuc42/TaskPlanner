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

        // Composite PK: User + Layer + LayerType (prevents duplicate membership rows)
        builder.HasKey(x => new { x.UserId, x.LayerId, x.LayerType });

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.LayerId).HasColumnName("layer_id").IsRequired();
        builder.Property(x => x.LayerType).HasColumnName("layer_type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.AccessLevel).HasColumnName("access_level").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.NotificationsEnabled).HasColumnName("notifications_enabled").IsRequired();

        // Indexes for common queries
        builder.HasIndex(x => x.LayerId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.LayerId, x.AccessLevel });
        builder.HasIndex(x => new { x.LayerId, x.LayerType });

    }
}
