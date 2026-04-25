using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Data.Configurations;

public class OutboxMessageConfiguration : EntityConfiguration<OutboxMessage>
{
    public override void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        base.Configure(builder);

        builder.ToTable("outbox_messages");

        builder.Property(x => x.Id).HasColumnName("id").HasColumnOrder(0);

        builder.Property(x => x.Type).IsRequired().HasMaxLength(256).HasColumnName("type");
        builder.Property(x => x.Content).IsRequired().HasColumnType("text").HasColumnName("content");
        builder.Property(x => x.OccurredOnUtc).IsRequired().HasColumnName("occurred_on_utc");
        builder.Property(x => x.ProcessedOnUtc).HasColumnName("processed_on_utc");
        builder.Property(x => x.Error).HasColumnType("text").HasColumnName("error");
        builder.Property(x => x.State).HasConversion<string>().HasMaxLength(50).HasColumnName("state").IsRequired();
        builder.Property(x => x.ErrorCount).HasDefaultValue(0).HasColumnName("error_count");
        builder.Property(x => x.ScheduledAtUtc).HasColumnName("scheduled_at_utc");

        builder.HasIndex(x => x.State);
        builder.HasIndex(x => x.OccurredOnUtc);
        builder.HasIndex(x => x.ScheduledAtUtc);
    }
}
