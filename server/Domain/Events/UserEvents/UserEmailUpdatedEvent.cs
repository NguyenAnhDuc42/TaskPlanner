using Domain.Common.Interfaces;
using System;

namespace Domain.Events.UserEvents;

public record UserEmailUpdatedEvent(Guid UserId, string NewEmail) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}