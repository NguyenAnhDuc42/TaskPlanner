using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Relationship;

namespace Infrastructure.Data.Configurations.Relationship;

public class ChatRoomMemberConfiguration : CompositeConfiguration<ChatRoomMember>
{
    public override void Configure(EntityTypeBuilder<ChatRoomMember> builder)
    {
        base.Configure(builder);

        builder.ToTable("chat_room_members");

        // Composite PK: ChatRoomId + UserId
        builder.HasKey(x => new { x.ChatRoomId, x.UserId });

        builder.Property(x => x.ChatRoomId).HasColumnName("chat_room_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.IsMuted).HasColumnName("is_muted").IsRequired();
        builder.Property(x => x.MuteEndTime).HasColumnName("mute_end_time");
        builder.Property(x => x.IsBanned).HasColumnName("is_banned").IsRequired();
        builder.Property(x => x.BannedAt).HasColumnName("banned_at");
        builder.Property(x => x.BannedBy).HasColumnName("banned_by");
        builder.Property(x => x.NotificationsEnabled).HasColumnName("notifications_enabled").IsRequired();

        // Indexes
        builder.HasIndex(x => x.ChatRoomId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.ChatRoomId, x.Role });
    }
}
