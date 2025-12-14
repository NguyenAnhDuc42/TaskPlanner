using System;
using Domain.Common.Interfaces;
using MediatR;

namespace Domain.Events;

public record class BaseDomainEvent : IDomainEvent,INotification
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid? AggregateId { get; init; } = null;
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    public long SequenceNumber { get; init; }

    protected BaseDomainEvent(Guid? aggregateId = null)
    {
        AggregateId = aggregateId;
    }
    protected BaseDomainEvent(Guid eventId, Guid? aggregateId, DateTimeOffset occurredOn, long sequenceNumber)
    {
        EventId = eventId;
        AggregateId = aggregateId;
        OccurredOn = occurredOn;
        SequenceNumber = sequenceNumber;
    }
}
