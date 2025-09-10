using System;
using Application.Common;

namespace Application.Interfaces.RealTime;

public interface IRealtimePublisher
{
    Task PublishAsync<TPayload>(EventEnvelope<TPayload> envelope, CancellationToken ct = default);

    // Typed convenience helpers (callers use these; they call PublishAsync under the hood)
    Task PublishCreatedAsync<TPayload>(Guid workspaceId, string entityType, Guid parentId, string? parentType, TPayload dto, Guid actorId, CancellationToken ct = default);
    Task PublishUpdatedAsync<TPayload>(Guid workspaceId, string entityType, TPayload dto, Guid actorId, CancellationToken ct = default);
    Task PublishDeletedAsync(Guid workspaceId, string entityType, Guid entityId, Guid actorId, CancellationToken ct = default);
    Task PublishReorderedAsync(Guid workspaceId, string entityType, Guid parentId, string? parentType, Guid[] orderedIds, Guid actorId, CancellationToken ct = default);
}
