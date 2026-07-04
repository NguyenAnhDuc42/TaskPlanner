using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Application;

[Authorize]
public class WorkspaceHub(TaskPlanDbContext db) : Hub
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

        var isMember = await db.WorkspaceMembers.AsNoTracking().AnyAsync(m =>
            m.ProjectWorkspaceId == workspaceId &&
            m.UserId == userId &&
            m.DeletedAt == null &&
            m.Status == MembershipStatus.Active);

        if (!isMember) return;

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

    // Previously accepted any userId with no check — any client could join another user's
    // personal notification group and receive their private notifications just by passing their
    // id. Now only ever joins the CALLER's own group, regardless of what userId is passed in —
    // kept the parameter so the frontend's existing invoke("JoinUser", userId) call doesn't need
    // to change, it just no longer has any effect beyond the caller's own identity.
    public Task JoinUser(Guid userId)
    {
        var callerId = CurrentUserId();
        if (callerId is null || callerId != userId) return Task.CompletedTask;

        return Groups.AddToGroupAsync(Context.ConnectionId, $"user:{callerId}");
    }
}
