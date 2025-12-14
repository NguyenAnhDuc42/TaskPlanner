using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Support.Notification;

namespace Infrastructure.Data.Configurations.Support.Notification;

public class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(EntityTypeBuilder<UserNotification> builder)
    {
        builder.ToTable("user_notifications");

        // Composite PK
        builder.HasKey(x => new { x.UserId, x.NotficationEventId });

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.NotficationEventId).HasColumnName("notification_event_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.ReadAt).HasColumnName("read_at");
        builder.Property(x => x.ArchivedAt).HasColumnName("archived_at");
        builder.Property(x => x.Chanel).HasColumnName("chanel").HasConversion<string>().HasMaxLength(50).IsRequired();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.NotficationEventId);
        builder.HasIndex(x => x.Status);
    }
}
