using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Application;

public class RealtimeService(
    IHubContext<WorkspaceHub> HubContext,
    IHubContext<SyncHub> SyncHubContext,
    IHttpContextAccessor HttpContextAccessor,
    ILogger<RealtimeService> Logger)
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

    public async Task NotifyWorkspaceAsync(Guid workspaceId, string eventName, object data, CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var senderConnectionId = GetSenderConnectionId();
            if (!string.IsNullOrEmpty(senderConnectionId))
            {
                await HubContext.Clients
                    .GroupExcept($"workspace:{workspaceId}", new[] { senderConnectionId })
                    .SendAsync(eventName, data, cancellationToken);
                Logger.LogInformation("[REALTIME] Successfully broadcasted '{EventName}' to workspace {WorkspaceId} (except sender {ConnectionId}) in {ElapsedMs}ms", eventName, workspaceId, senderConnectionId, sw.ElapsedMilliseconds);
            }
            else
            {
                await HubContext.Clients
                    .Group($"workspace:{workspaceId}")
                    .SendAsync(eventName, data, cancellationToken);
                Logger.LogInformation("[REALTIME] Successfully broadcasted '{EventName}' to workspace {WorkspaceId} (all connected clients) in {ElapsedMs}ms", eventName, workspaceId, sw.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REALTIME] FAILED to broadcast '{EventName}' to workspace {WorkspaceId} after {ElapsedMs}ms", eventName, workspaceId, sw.ElapsedMilliseconds);
        }
    }

    public async Task NotifyUserAsync(Guid userId, string eventName, object data, CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var senderConnectionId = GetSenderConnectionId();
            if (!string.IsNullOrEmpty(senderConnectionId))
            {
                await HubContext.Clients
                    .GroupExcept($"user:{userId}", new[] { senderConnectionId })
                    .SendAsync(eventName, data, cancellationToken);
                Logger.LogInformation("[REALTIME] Successfully broadcasted '{EventName}' to user {UserId} (except sender {ConnectionId}) in {ElapsedMs}ms", eventName, userId, senderConnectionId, sw.ElapsedMilliseconds);
            }
            else
            {
                await HubContext.Clients
                    .Group($"user:{userId}")
                    .SendAsync(eventName, data, cancellationToken);
                Logger.LogInformation("[REALTIME] Successfully broadcasted '{EventName}' to user {UserId} (all connected clients) in {ElapsedMs}ms", eventName, userId, sw.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REALTIME] FAILED to broadcast '{EventName}' to user {UserId} after {ElapsedMs}ms", eventName, userId, sw.ElapsedMilliseconds);
        }
    }

    public async Task NotifySyncEventAsync(Guid workspaceId, SyncEventPayload payload, CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var senderConnectionId = GetSenderConnectionId();
            if (!string.IsNullOrEmpty(senderConnectionId))
            {
                await SyncHubContext.Clients
                    .GroupExcept($"workspace:{workspaceId}", new[] { senderConnectionId })
                    .SendAsync("Delta", payload, cancellationToken);
            }
            else
            {
                await SyncHubContext.Clients
                    .Group($"workspace:{workspaceId}")
                    .SendAsync("Delta", payload, cancellationToken);
            }
            Logger.LogInformation("[REALTIME] Broadcasted sync Delta (syncId {SyncId}) to workspace {WorkspaceId} in {ElapsedMs}ms", payload.SyncId, workspaceId, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REALTIME] FAILED to broadcast sync Delta to workspace {WorkspaceId} after {ElapsedMs}ms", workspaceId, sw.ElapsedMilliseconds);
        }
    }

    public async Task NotifySyncEventBatchAsync(Guid workspaceId, IReadOnlyList<SyncEventPayload> payloads, CancellationToken cancellationToken = default)
    {
        if (payloads.Count == 0) return;
        if (payloads.Count == 1)
        {
            await NotifySyncEventAsync(workspaceId, payloads[0], cancellationToken);
            return;
        }

        var batch = new SyncDeltaBatch(
            Actions: [.. payloads],
            DatabaseVersion: SyncQueryService.CurrentDatabaseVersion,
            LatestSyncId: payloads[^1].SyncId
        );

        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var senderConnectionId = GetSenderConnectionId();
            if (!string.IsNullOrEmpty(senderConnectionId))
            {
                await SyncHubContext.Clients
                    .GroupExcept($"workspace:{workspaceId}", new[] { senderConnectionId })
                    .SendAsync("DeltaBatch", batch, cancellationToken);
            }
            else
            {
                await SyncHubContext.Clients
                    .Group($"workspace:{workspaceId}")
                    .SendAsync("DeltaBatch", batch, cancellationToken);
            }
            Logger.LogInformation("[REALTIME] Broadcasted sync DeltaBatch ({Count} events) to workspace {WorkspaceId} in {ElapsedMs}ms", payloads.Count, workspaceId, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REALTIME] FAILED to broadcast sync DeltaBatch to workspace {WorkspaceId} after {ElapsedMs}ms", workspaceId, sw.ElapsedMilliseconds);
        }
    }

    public async Task NotifyEntitiesUpdatedAsync(Guid workspaceId, EntityBatchUpdate payload, CancellationToken cancellationToken = default)
    {
        await NotifyWorkspaceAsync(workspaceId, "EntitiesUpdated", payload, cancellationToken);
    }

    public async Task NotifyEntitiesDeletedAsync(Guid workspaceId, EntityBatchDelete payload, CancellationToken cancellationToken = default)
    {
        await NotifyWorkspaceAsync(workspaceId, "EntitiesDeleted", payload, cancellationToken);
    }
}
