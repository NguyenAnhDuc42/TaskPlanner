using Domain.Common.Interfaces;

namespace Application.Common.Interfaces;

public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task Handle(TEvent notification, CancellationToken cancellationToken);
}
