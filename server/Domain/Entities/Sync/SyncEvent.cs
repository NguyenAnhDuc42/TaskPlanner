namespace Domain;

public class SyncEvent : ITenanted
{
    public long Id { get; set; } // Auto-incrementing primary key (lastSyncId)
    public Guid ProjectWorkspaceId { get; set; } // ITenanted requirement
    public SyncEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    public SyncAction Action { get; set; }
    public string Payload { get; set; } = string.Empty; // Partial JSON payload
    public string ClientTraceId { get; set; } = string.Empty;
    public Guid AuthorUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsPublished { get; set; } = false; // For Background Worker
}
