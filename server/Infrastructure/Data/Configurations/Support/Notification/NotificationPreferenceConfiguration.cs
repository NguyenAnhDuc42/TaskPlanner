using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Support.Notification;

namespace Infrastructure.Data.Configurations.Support.Notification;

public class NotificationPreferenceConfiguration : EntityConfiguration<NotificationPreference>
{
    public override void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        base.Configure(builder);

        builder.ToTable("notification_preferences");

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.ScopeType).HasColumnName("scope_type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.ScopeId).HasColumnName("scope_id");
        builder.Property(x => x.EnabledEventTypes).HasColumnName("enabled_event_types").HasMaxLength(1000);
        builder.Property(x => x.NotificationFrequency).HasColumnName("notification_frequency").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.NotificationChannels).HasColumnName("notification_channels").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.IsMuted).HasColumnName("is_muted").IsRequired();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.ScopeType, x.ScopeId }).IsUnique();
    }
}
