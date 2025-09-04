using System;
using Domain.Common.Interfaces;
using MediatR;

namespace Infrastructure.Events.DomainEvents;

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;
    public DomainEventDispatcher(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        if (domainEvents == null || !domainEvents.Any()) return;

        foreach (var domainEvent in domainEvents)
        {
            var wrapperType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
            var wrapper = Activator.CreateInstance(wrapperType, domainEvent);
            if (wrapper is INotification wrappedNotification)
            {
                await _mediator.Publish(wrappedNotification, cancellationToken).ConfigureAwait(false);
            }
        }

    }
}

public sealed class DomainEventNotification<TDomainEvent> : INotification where TDomainEvent : IDomainEvent
{
    public TDomainEvent Event { get; }
    public DomainEventNotification(TDomainEvent @event) => Event = @event;
}
