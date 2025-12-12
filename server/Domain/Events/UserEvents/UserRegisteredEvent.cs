using System;
using Domain.Common.Interfaces;

namespace Domain.Events.UserEvents;

public record UserRegisteredEvent(
    Guid UserId, 
    string Email, 
    string Username,
    string? EmailVerificationToken,
    DateTimeOffset OccurredOn
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public Guid? AggregateId => UserId;
    public long SequenceNumber { get; init; }
}
