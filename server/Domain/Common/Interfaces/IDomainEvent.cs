using MediatR;

namespace Domain.Common.Interfaces;

public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTimeOffset OccurredOn { get; }
    Guid? AggregateId { get; }
    long SequenceNumber { get; }
}
