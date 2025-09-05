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
        if (domainEvents is null)
            throw new ArgumentNullException(nameof(domainEvents));
        if (!domainEvents.Any()) return;

        foreach (var domainEvent in domainEvents)
        {
            var wrapperType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
            var wrapper = Activator.CreateInstance(wrapperType, domainEvent);
            var tasks = domainEvents
            .Where(e => e is not null)
            .Select(e =>
            {
                var wrapperType = typeof(DomainEventNotification<>).MakeGenericType(e.GetType());
                var wrapper = Activator.CreateInstance(wrapperType, e);
                return wrapper is INotification notification
                    ? _mediator.Publish(notification, cancellationToken)
                    : Task.CompletedTask;
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

    }
}

public sealed class DomainEventNotification<TDomainEvent> : INotification where TDomainEvent : IDomainEvent
{
    public TDomainEvent Event { get; }
    public DomainEventNotification(TDomainEvent @event) => Event = @event;
}
