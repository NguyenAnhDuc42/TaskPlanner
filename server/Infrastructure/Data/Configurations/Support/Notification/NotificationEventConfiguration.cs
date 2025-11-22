using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Support.Notification;

namespace Infrastructure.Data.Configurations.Support.Notification;

public class NotificationEventConfiguration : EntityConfiguration<NotificationEvent>
{
    public override void Configure(EntityTypeBuilder<NotificationEvent> builder)
    {
        base.Configure(builder);

        builder.ToTable("notification_events");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.SourceId).HasColumnName("source_id").IsRequired();
        builder.Property(x => x.SourceType).HasColumnName("source_type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(100).IsRequired();
        builder.Property(x => x.ActorId).HasColumnName("actor_id");
        builder.Property(x => x.Payload).HasColumnName("payload").HasColumnType("text"); // or jsonb if supported
        builder.Property(x => x.IsCritical).HasColumnName("is_critical").IsRequired();
        builder.Property(x => x.AggregationKey).HasColumnName("aggregation_key").HasMaxLength(100);
        builder.Property(x => x.IsAggregated).HasColumnName("is_aggregated").IsRequired();
        builder.Property(x => x.AggregatedIntoEventId).HasColumnName("aggregated_into_event_id");

        builder.HasIndex(x => x.WorkspaceId);
        builder.HasIndex(x => new { x.SourceId, x.SourceType });
        builder.HasIndex(x => x.EventType);
    }
}
