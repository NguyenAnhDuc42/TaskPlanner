using Domain.Common.Interfaces;
using System;

namespace Domain.Events.UserEvents;

public record UserCreatedEvent(Guid UserId, string Name, string Email) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}