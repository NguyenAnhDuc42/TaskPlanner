using System;

namespace Domain.Common.Interfaces;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredOn { get; }
    Guid? AggregateId { get; }
    long SequenceNumber { get; }
}
