using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Support.Notification;

namespace Infrastructure.Data.Configurations.Support.Notification;

public class NotificationDeliveryConfiguration : EntityConfiguration<NotificationDelivery>
{
    public override void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        base.Configure(builder);

        builder.ToTable("notification_deliveries");

        builder.Property(x => x.UserNotificationId).HasColumnName("user_notification_id").IsRequired();
        builder.Property(x => x.Chanel).HasColumnName("chanel").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.SentAt).HasColumnName("sent_at");
        builder.Property(x => x.FailedAt).HasColumnName("failed_at");
        builder.Property(x => x.FailureReason).HasColumnName("failure_reason").HasMaxLength(500);
        builder.Property(x => x.RetryCount).HasColumnName("retry_count").IsRequired();
        builder.Property(x => x.LastRetryAt).HasColumnName("last_retry_at");
        builder.Property(x => x.Metadate).HasColumnName("metadate").HasMaxLength(1000);

        builder.HasIndex(x => x.UserNotificationId);
        builder.HasIndex(x => x.Status);
    }
}
