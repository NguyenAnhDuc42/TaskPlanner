using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.ProjectEntities;

namespace Infrastructure.Data.Configurations.ProjectEntities;

public class StatusConfiguration : EntityConfiguration<Status>
{
    public override void Configure(EntityTypeBuilder<Status> builder)
    {
        base.Configure(builder);

        builder.ToTable("statuses");

        builder.Property(x => x.LayerId).HasColumnName("layer_id");
        builder.Property(x => x.LayerType).HasColumnName("layer_type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Color).HasColumnName("color").HasMaxLength(32).IsRequired();
        builder.Property(x => x.Category).HasColumnName("category").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.OrderKey).HasColumnName("order_key").IsRequired();
        builder.Property(x => x.IsDefaultStatus).HasColumnName("is_default_status").IsRequired();

        // Indexes
        builder.HasIndex(x => new { x.LayerId, x.LayerType });
        builder.HasIndex(x => new { x.LayerId, x.LayerType, x.OrderKey });
    }
}
