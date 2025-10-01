
using Domain.OutBox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class DeadLetterMessageConfiguration : IEntityTypeConfiguration<DeadLetterMessage>
    {
        public void Configure(EntityTypeBuilder<DeadLetterMessage> builder)
        {
            builder.ToTable("dead_letter_messages");

            builder.HasKey(e => e.Id);

            // Query optimization for unreplayed messages
            builder.HasIndex(e => e.IsReplayed)
                   .HasDatabaseName("idx_dead_letter_replayed")
                   .HasFilter("is_replayed = false");

            // Query by event type
            builder.HasIndex(e => e.EventType)
                   .HasDatabaseName("idx_dead_letter_event_type");

            // Query by dead letter timestamp
            builder.HasIndex(e => e.DeadLetteredAtUtc)
                   .HasDatabaseName("idx_dead_letter_timestamp");

            builder.Property(e => e.Id)
                   .ValueGeneratedNever();

            builder.Property(e => e.EventType)
                   .IsRequired()
                   .HasMaxLength(200)
                   .HasColumnName("event_type");

            builder.Property(e => e.Payload)
                   .IsRequired()
                   .HasColumnType("text")
                   .HasColumnName("payload");

            builder.Property(e => e.Reason)
                   .IsRequired()
                   .HasColumnType("text")
                   .HasColumnName("reason");

            builder.Property(e => e.OccurredOnUtc)
                   .IsRequired()
                   .HasColumnName("occurred_on_utc");

            builder.Property(e => e.DeadLetteredAtUtc)
                   .IsRequired()
                   .HasColumnName("dead_lettered_at_utc");

            builder.Property(e => e.IsReplayed)
                   .IsRequired()
                   .HasColumnName("is_replayed");

            builder.Property(e => e.ReplayedAtUtc)
                   .HasColumnName("replayed_at_utc");

            builder.Property(e => e.TraceId)
                   .HasMaxLength(100)
                   .HasColumnName("trace_id");
        }
    }
}