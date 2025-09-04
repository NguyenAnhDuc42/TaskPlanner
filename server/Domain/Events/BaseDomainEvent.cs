using System;
using Domain.Common.Interfaces;

namespace Domain.Events;

public class BaseDomainEvent(Guid? AggregateId = null) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid? AggregateId { get; init; } = AggregateId;
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    public long SequenceNumber { get; init; }
}
