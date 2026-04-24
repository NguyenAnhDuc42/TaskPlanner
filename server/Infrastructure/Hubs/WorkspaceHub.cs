using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Hubs;

public class WorkspaceHub : Hub
{
    public async Task JoinWorkspace(Guid workspaceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"workspace:{workspaceId}");
    }
    public async Task LeaveWorkspace(Guid workspaceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"workspace:{workspaceId}");
    }

    public async Task JoinDashboard(Guid dashboardId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"dashboard:{dashboardId}");
    }

    public async Task LeaveDashboard(Guid dashboardId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"dashboard:{dashboardId}");
    }

    public async Task JoinUser(Guid userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
    }

}
