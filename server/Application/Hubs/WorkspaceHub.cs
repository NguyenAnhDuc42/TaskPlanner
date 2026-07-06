using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Application;

[Authorize]
public class WorkspaceHub(WorkspaceMembershipResolver membershipResolver) : Hub
{
    private Guid? CurrentUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirst("sub");
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : null;
    }

    // Without a membership check, any authenticated user could join any workspace's broadcast
    // group just by passing its id — the same class of gap SyncHub had (see SyncHub.cs).
    public async Task JoinWorkspace(Guid workspaceId)
    {
        var userId = CurrentUserId();
        if (userId is null) return;

        var activeMember = await membershipResolver.ResolveActiveMemberAsync(workspaceId, userId.Value);
        if (activeMember is null) return;

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

       public Task JoinUser(Guid userId)
    {
        var callerId = CurrentUserId();
        if (callerId is null || callerId != userId) return Task.CompletedTask;

        return Groups.AddToGroupAsync(Context.ConnectionId, $"user:{callerId}");
    }
}
