using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Hubs;

/// <summary>
/// SignalR Hub for workspace real-time updates.
/// Clients join workspace groups to receive updates.
/// </summary>
public class WorkspaceHub : Hub
{
    /// <summary>
    /// Client joins a workspace group to receive updates.
    /// Call this when user opens a workspace.
    /// </summary>
    public async Task JoinWorkspace(Guid workspaceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"workspace:{workspaceId}");
    }

    /// <summary>
    /// Client leaves a workspace group.
    /// Call this when user closes a workspace.
    /// </summary>
    public async Task LeaveWorkspace(Guid workspaceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"workspace:{workspaceId}");
    }

    /// <summary>
    /// Client joins their personal notification group.
    /// Call this on connection.
    /// </summary>
    public async Task JoinUser(Guid userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
    }

    /// <summary>
    /// Client joins a chat room group to receive messages.
    /// Call this when user opens a chat room.
    /// </summary>
    public async Task JoinChatRoom(Guid chatRoomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"chat:{chatRoomId}");
    }

    /// <summary>
    /// Client leaves a chat room group.
    /// Call this when user closes a chat room.
    /// </summary>
    public async Task LeaveChatRoom(Guid chatRoomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat:{chatRoomId}");
    }
}
