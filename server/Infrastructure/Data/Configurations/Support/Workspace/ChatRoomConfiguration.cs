using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Support.Workspace;

namespace Infrastructure.Data.Configurations.Support.Workspace;

public class ChatRoomConfiguration : EntityConfiguration<ChatRoom>
{
    public override void Configure(EntityTypeBuilder<ChatRoom> builder)
    {
        base.Configure(builder);

        builder.ToTable("chat_rooms");

        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.ProjectWorkspaceId).HasColumnName("project_workspace_id").IsRequired();
        builder.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(500);
        builder.Property(x => x.IsPrivate).HasColumnName("is_private").IsRequired();
        builder.Property(x => x.IsArchived).HasColumnName("is_archived").IsRequired();

        // Navigation to Messages
        builder.HasMany(x => x.Messages)
            .WithOne(m => m.ChatRoom)
            .HasForeignKey(m => m.ChatRoomId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation to Members
        builder.HasMany(x => x.Members)
            .WithOne(m => m.ChatRoom)
            .HasForeignKey(m => m.ChatRoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(ChatRoom.Messages))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(ChatRoom.Members))?.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.ProjectWorkspaceId);
    }
}
