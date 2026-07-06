using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Application;

[Authorize]
public class SyncHub(WorkspaceMembershipResolver membershipResolver, SyncQueryService syncQueryService, ILogger<SyncHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var workspaceIdRaw = httpContext?.Request.Query["workspaceId"].ToString();
        var lastSyncIdRaw = httpContext?.Request.Query["lastSyncId"].ToString();

        if (!Guid.TryParse(workspaceIdRaw, out var workspaceId))
        {
            logger.LogWarning("SyncHub connection rejected — invalid or missing workspaceId");
            Context.Abort();
            return;
        }

        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirst("sub");
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            logger.LogWarning("SyncHub connection rejected — no valid user id claim");
            Context.Abort();
            return;
        }


        var activeMember = await membershipResolver.ResolveActiveMemberAsync(workspaceId, userId);

        if (activeMember is null)
        {
            logger.LogWarning("SyncHub connection rejected — user {UserId} is not an active member of workspace {WorkspaceId}", userId, workspaceId);
            Context.Abort();
            return;
        }

        long.TryParse(lastSyncIdRaw, out var lastSyncId);

        await Groups.AddToGroupAsync(Context.ConnectionId, $"workspace:{workspaceId}");

        // Catch-up: send everything the client missed since lastSyncId
        var batch = await syncQueryService.GetChangesAsync(workspaceId, lastSyncId);
        await Clients.Caller.SendAsync("DeltaBatch", batch);

        await base.OnConnectedAsync();
    }
}
