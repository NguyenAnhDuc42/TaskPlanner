using System.Collections.Concurrent;
using System.Reflection;
using Application.Common.Interfaces;
using Application.Interfaces;
using Domain.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Events.DomainEvents;

public class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<(Type HandlerType, Type EventType), MethodInfo> MethodCache = new();

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        if (domainEvents is null) throw new ArgumentNullException(nameof(domainEvents));
        if (!domainEvents.Any()) return;

        foreach (var domainEvent in domainEvents)
        {
            if (domainEvent is null) continue;

            var eventType = domainEvent.GetType();
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            
            var handlers = serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                if (handler != null)
                {
                    var handleMethod = MethodCache.GetOrAdd((handlerType, eventType), key => 
                        key.HandlerType.GetMethod("Handle", new[] { key.EventType, typeof(CancellationToken) })!);

                    if (handleMethod != null)
                    {
                        var task = (Task)handleMethod.Invoke(handler, new object[] { domainEvent, cancellationToken })!;
                        await task.ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
