using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Application;

public class SyncHub(SyncQueryService syncQueryService, ILogger<SyncHub> logger) : Hub
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

        long.TryParse(lastSyncIdRaw, out var lastSyncId);

        await Groups.AddToGroupAsync(Context.ConnectionId, $"workspace:{workspaceId}");

        // Catch-up: send everything the client missed since lastSyncId
        var batch = await syncQueryService.GetChangesAsync(workspaceId, lastSyncId);
        await Clients.Caller.SendAsync("DeltaBatch", batch);

        await base.OnConnectedAsync();
    }
}
