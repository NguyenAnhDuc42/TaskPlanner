using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace Application;

public class RealtimeService(IHubContext<WorkspaceHub> HubContext, IHttpContextAccessor HttpContextAccessor)
{
    private string? GetSenderConnectionId()
    {
        var httpContext = HttpContextAccessor.HttpContext;
        if (httpContext != null && httpContext.Request.Headers.TryGetValue("X-Connection-Id", out var connectionId))
        {
            return connectionId.ToString();
        }
        return null;
    }

    public async Task NotifyWorkspaceAsync(Guid workspaceId, string eventName, object data, CancellationToken ct = default)
    {
        var senderConnectionId = GetSenderConnectionId();
        if (!string.IsNullOrEmpty(senderConnectionId))
        {
            await HubContext.Clients
                .GroupExcept($"workspace:{workspaceId}", new[] { senderConnectionId })
                .SendAsync(eventName, data, ct);
        }
        else
        {
            await HubContext.Clients
                .Group($"workspace:{workspaceId}")
                .SendAsync(eventName, data, ct);
        }
    }

    public async Task NotifyUserAsync(Guid userId, string eventName, object data, CancellationToken ct = default)
    {
        var senderConnectionId = GetSenderConnectionId();
        if (!string.IsNullOrEmpty(senderConnectionId))
        {
            await HubContext.Clients
                .GroupExcept($"user:{userId}", new[] { senderConnectionId })
                .SendAsync(eventName, data, ct);
        }
        else
        {
            await HubContext.Clients
                .Group($"user:{userId}")
                .SendAsync(eventName, data, ct);
        }
    }

    public async Task NotifyEntitiesUpdatedAsync(Guid workspaceId, EntityBatchUpdate payload, CancellationToken ct = default)
    {
        await NotifyWorkspaceAsync(workspaceId, "EntitiesUpdated", payload, ct);
    }

    public async Task NotifyEntitiesDeletedAsync(Guid workspaceId, EntityBatchDelete payload, CancellationToken ct = default)
    {
        await NotifyWorkspaceAsync(workspaceId, "EntitiesDeleted", payload, ct);
    }
}
