using Domain.Common.Interfaces;
using System;

namespace Domain.Events.SessionEvents;

public record SessionCreatedEvent(Guid SessionId, Guid UserId, DateTimeOffset ExpiresAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public record SessionRevokedEvent(Guid SessionId, Guid UserId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public record SessionExpirationExtendedEvent(Guid SessionId, Guid UserId, DateTimeOffset NewExpiresAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}