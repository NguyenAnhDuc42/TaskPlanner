using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Support.Workspace;

namespace Infrastructure.Data.Configurations.Support.Workspace;

public class ChatMessageConfiguration : EntityConfiguration<ChatMessage>
{
    public override void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        base.Configure(builder);

        builder.ToTable("chat_messages");

        builder.Property(x => x.ChatRoomId).HasColumnName("chat_room_id").IsRequired();
        builder.Property(x => x.SenderId).HasColumnName("sender_id").IsRequired();
        builder.Property(x => x.Content).HasColumnName("content").HasMaxLength(4000).IsRequired();
        builder.Property(x => x.IsEdited).HasColumnName("is_edited").IsRequired();
        builder.Property(x => x.EditedAt).HasColumnName("edited_at");
        builder.Property(x => x.IsPinned).HasColumnName("is_pinned").IsRequired();
        builder.Property(x => x.HasAttachment).HasColumnName("has_attachment").IsRequired();
        builder.Property(x => x.ReplyToMessageId).HasColumnName("reply_to_message_id");
        builder.Property(x => x.ReactionCount).HasColumnName("reaction_count").IsRequired();

        // Self-referencing relationship for replies
        builder.HasOne(x => x.ReplyToMessage)
            .WithMany()
            .HasForeignKey(x => x.ReplyToMessageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ChatRoomId);
        builder.HasIndex(x => x.SenderId);
    }
}
