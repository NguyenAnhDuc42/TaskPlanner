namespace Application;

public record NotificationRecord
{
    public Guid Id { get; init; }
    public string Type { get; init; } = null!;
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public Guid? WorkspaceId { get; init; }
    public string Title { get; init; } = null!;
    public string? Body { get; init; }
    public bool IsRead { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    // Actor info for display
    public string? ActorName { get; init; }
}
