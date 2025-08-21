using Domain.Common.Interfaces;
using System;

namespace Domain.Events.UserEvents;

public record UserPasswordChangedEvent(Guid UserId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}