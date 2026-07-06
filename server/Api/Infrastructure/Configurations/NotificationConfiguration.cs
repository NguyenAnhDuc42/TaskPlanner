using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Api;

public class NotificationConfiguration : EntityConfiguration<Notification>
{
    public override void Configure(EntityTypeBuilder<Notification> builder)
    {
        base.Configure(builder);

        builder.ToTable("notifications");

        builder.Property(x => x.RecipientUserId).HasColumnName("recipient_user_id").IsRequired();
        builder.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
        builder.Property(x => x.ProjectWorkspaceId).HasColumnName("project_workspace_id");
        builder.Property(x => x.Type).HasColumnName("type").IsRequired();
        builder.Property(x => x.EntityType).HasColumnName("entity_type");
        builder.Property(x => x.EntityId).HasColumnName("entity_id");
        builder.Property(x => x.Title).HasColumnName("title").IsRequired();
        builder.Property(x => x.Body).HasColumnName("body");
        builder.Property(x => x.IsRead).HasColumnName("is_read").HasDefaultValue(false);

        builder.HasIndex(x => new { x.RecipientUserId, x.IsRead, x.CreatedAt });
        builder.HasIndex(x => x.ProjectWorkspaceId);
    }
}
