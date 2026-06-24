using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Application;

public class NotificationService(TaskPlanDbContext db, RealtimeService realtime)
{
    public async Task PushAsync(
        Guid recipientUserId,
        Guid? actorUserId,
        Guid? projectWorkspaceId,
        string type,
        string? entityType,
        Guid? entityId,
        string title,
        string? body = null,
        CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var connectionString = db.Database.GetConnectionString();
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);
        await conn.ExecuteAsync(@"
            INSERT INTO notifications
                (id, recipient_user_id, actor_user_id, project_workspace_id, type, entity_type, entity_id, title, body, is_read, created_at, updated_at)
            VALUES
                (@Id, @RecipientUserId, @ActorUserId, @ProjectWorkspaceId, @Type, @EntityType, @EntityId, @Title, @Body, false, @Now, @Now)",
            new { Id = id, RecipientUserId = recipientUserId, ActorUserId = actorUserId, ProjectWorkspaceId = projectWorkspaceId, Type = type, EntityType = entityType, EntityId = entityId, Title = title, Body = body, Now = now });

        var record = new NotificationRecord
        {
            Id = id,
            Type = type,
            EntityType = entityType,
            EntityId = entityId,
            WorkspaceId = projectWorkspaceId,
            Title = title,
            Body = body,
            IsRead = false,
            CreatedAt = now,
        };

        _ = realtime.NotifyUserAsync(recipientUserId, "NewNotification", record, cancellationToken);
    }
}
