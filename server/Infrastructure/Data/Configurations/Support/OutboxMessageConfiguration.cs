using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Support;

namespace Infrastructure.Data.Configurations.Support;

public class OutboxMessageConfiguration : CompositeConfiguration<OutboxMessage>
{
    public override void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        base.Configure(builder);

        builder.ToTable("outbox_messages");

        builder.Property(x => x.Type).IsRequired().HasMaxLength(256).HasColumnName("type");
        builder.Property(x => x.Content).IsRequired().HasColumnType("text").HasColumnName("content");
        builder.Property(x => x.OccurredOnUtc).IsRequired().HasColumnName("occurred_on_utc");
        builder.Property(x => x.ProcessedOnUtc).HasColumnName("processed_on_utc");
        builder.Property(x => x.Error).HasColumnType("text").HasColumnName("error");
        builder.Property(x => x.State).HasConversion<string>().HasMaxLength(50).HasColumnName("state").IsRequired();

        builder.HasIndex(x => x.State);
        builder.HasIndex(x => x.OccurredOnUtc);
    }
}
