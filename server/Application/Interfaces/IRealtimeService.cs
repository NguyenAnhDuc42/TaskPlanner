namespace Application.Interfaces;

/// <summary>
/// Service for pushing real-time updates to connected clients.
/// </summary>
public interface IRealtimeService
{
    /// <summary>
    /// Notify all clients viewing a specific workspace.
    /// </summary>
    Task NotifyWorkspaceAsync(Guid workspaceId, string eventName, object data, CancellationToken ct = default);

    /// <summary>
    /// Notify a specific user (e.g., for personal notifications).
    /// </summary>
    Task NotifyUserAsync(Guid userId, string eventName, object data, CancellationToken ct = default);

    /// <summary>
    /// Notify all clients in a specific chat room.
    /// </summary>
    Task NotifyChatRoomAsync(Guid chatRoomId, string eventName, object data, CancellationToken ct = default);
}
