using Domain.OutBox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("outbox_messages");

            builder.HasKey(e => e.Id);

            // Performance index for worker queries
            builder.HasIndex(e => new { e.State, e.AvailableAtUtc })
                   .HasDatabaseName("idx_outbox_pending")
                   .HasFilter("state = 0"); // Pending only

            // Deduplication enforcement - CRITICAL
            builder.HasIndex(e => e.DeduplicationKey)
                   .IsUnique()
                   .HasDatabaseName("idx_outbox_deduplication")
                   .HasFilter("deduplication_key IS NOT NULL AND state = 0");

            builder.Property(e => e.Id)
                   .ValueGeneratedNever(); // Set by domain

            builder.Property(e => e.EventType)
                   .IsRequired()
                   .HasMaxLength(200)
                   .HasColumnName("event_type");

            builder.Property(e => e.Payload)
                   .IsRequired()
                   .HasColumnType("text")
                   .HasColumnName("payload");

            builder.Property(e => e.RoutingKey)
                   .HasMaxLength(200)
                   .HasColumnName("routing_key");

            builder.Property(e => e.DeduplicationKey)
                   .HasMaxLength(500)
                   .HasColumnName("deduplication_key");

            builder.Property(e => e.CreatedBy)
                   .HasMaxLength(200)
                   .HasColumnName("created_by");

            builder.Property(e => e.Attempts)
                   .IsRequired()
                   .HasColumnName("attempts");

            builder.Property(e => e.AvailableAtUtc)
                   .IsRequired()
                   .HasColumnName("available_at_utc");

            builder.Property(e => e.OccurredOnUtc)
                   .IsRequired()
                   .HasColumnName("occurred_on_utc");

            builder.Property(e => e.SentOnUtc)
                   .HasColumnName("sent_on_utc");

            builder.Property(e => e.ProcessedOnUtc)
                   .HasColumnName("processed_on_utc");

            builder.Property(e => e.LastError)
                   .HasColumnType("text")
                   .HasColumnName("last_error");

            builder.Property(e => e.State)
                   .IsRequired()
                   .HasConversion<int>()
                   .HasColumnName("state");
        }
    }
}