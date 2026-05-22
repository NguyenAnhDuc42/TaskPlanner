using Microsoft.AspNetCore.SignalR;

namespace Application;

public class RealtimeService(IHubContext<WorkspaceHub> HubContext)
{
    public async Task NotifyWorkspaceAsync(Guid workspaceId, string eventName, object data, CancellationToken ct = default)
    {
        await HubContext.Clients
            .Group($"workspace:{workspaceId}")
            .SendAsync(eventName, data, ct);
    }

    public async Task NotifyUserAsync(Guid userId, string eventName, object data, CancellationToken ct = default)
    {
        await HubContext.Clients
            .Group($"user:{userId}")
            .SendAsync(eventName, data, ct);
    }
}





