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
}
