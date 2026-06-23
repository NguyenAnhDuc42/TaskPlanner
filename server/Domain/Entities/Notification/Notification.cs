namespace Domain;

public class Notification : Entity
{
    public Guid RecipientUserId { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public Guid? ProjectWorkspaceId { get; private set; }
    public string Type { get; private set; } = null!;
    public string? EntityType { get; private set; }
    public Guid? EntityId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Body { get; private set; }
    public bool IsRead { get; private set; }

    private Notification() { }

    public static Notification Create(
        Guid recipientUserId,
        Guid? actorUserId,
        Guid? workspaceId,
        string type,
        string? entityType,
        Guid? entityId,
        string title,
        string? body = null)
    {
        var n = new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = recipientUserId,
            ActorUserId = actorUserId,
            ProjectWorkspaceId = workspaceId,
            Type = type,
            EntityType = entityType,
            EntityId = entityId,
            Title = title,
            Body = body,
            IsRead = false,
        };
        return n;
    }

    public void MarkRead()
    {
        IsRead = true;
        UpdateTimestamp();
    }
}
