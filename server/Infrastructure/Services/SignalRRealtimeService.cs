using Application.Interfaces;
using Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Services;

/// <summary>
/// SignalR implementation of IRealtimeService.
/// Pushes events to connected clients via WorkspaceHub.
/// </summary>
public class SignalRRealtimeService : IRealtimeService
{
    private readonly IHubContext<WorkspaceHub> _hubContext;

    public SignalRRealtimeService(IHubContext<WorkspaceHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyWorkspaceAsync(Guid workspaceId, string eventName, object data, CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"workspace:{workspaceId}")
            .SendAsync(eventName, data, ct);
    }

    public async Task NotifyUserAsync(Guid userId, string eventName, object data, CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"user:{userId}")
            .SendAsync(eventName, data, ct);
    }
}
