using System;
using Application.Common;
using Application.Interfaces.RealTime;
using Infrastructure.Services.RealTime.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Services.RealTime;

public class RealtimePublisher : IRealtimePublisher
{
    private readonly IHubContext<WorkspaceHub> _hub;
    public RealtimePublisher(IHubContext<WorkspaceHub> hub)
    {
        _hub = hub ?? throw new ArgumentNullException(nameof(hub));
    }
    private static string WorkspaceGroup(Guid workspaceId) => $"workspace_{workspaceId}";


    public Task PublishAsync<TPayload>(EventEnvelope<TPayload> envelope, CancellationToken ct = default)
    {
        if (envelope == null) throw new ArgumentNullException(nameof(envelope));
        return _hub.Clients.Group(WorkspaceGroup(envelope.WorkspaceId))
        .SendAsync(envelope.EventName, envelope, ct);
    }

    public Task PublishCreatedAsync<TPayload>(Guid workspaceId, string entityType, Guid parentId, string? parentType, TPayload dto, Guid actorId, CancellationToken ct = default)
    {
        var eventName = $"{entityType}.Created";
        var env = new EventEnvelope<TPayload>(workspaceId, eventName, dto, actorId, DateTime.UtcNow, 1, parentType, parentId);
        return PublishAsync(env, ct);

    }

    public Task PublishDeletedAsync(Guid workspaceId, string entityType, Guid entityId, Guid actorId, CancellationToken ct = default)
    {
        var eventName = $"{entityType}.Deleted";
        var payload = new { Id = entityId };
        var env = new EventEnvelope<object>(workspaceId, eventName, payload, actorId, DateTime.UtcNow);
        return PublishAsync(env, ct);
    }

    public Task PublishReorderedAsync(Guid workspaceId, string entityType, Guid parentId, string? parentType, Guid[] orderedIds, Guid actorId, CancellationToken ct = default)
    {
        var eventName = $"{entityType}.Reordered";
        var payload = new { ParentId = parentId, ParentType = parentType, OrderedIds = orderedIds };
        var env = new EventEnvelope<object>(workspaceId, eventName, payload, actorId, DateTime.UtcNow);
        return PublishAsync(env, ct);
    }

    public Task PublishUpdatedAsync<TPayload>(Guid workspaceId, string entityType, TPayload dto, Guid actorId, CancellationToken ct = default)
    {
        var eventName = $"{entityType}.Updated";
        var env = new EventEnvelope<TPayload>(workspaceId, eventName, dto, actorId, DateTime.UtcNow);
        return PublishAsync(env, ct);

    }
}
