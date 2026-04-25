using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Background.Interfaces;
using Domain.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Services.Background;

public class BackgroundEventDispatcher : IBackgroundEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly ConcurrentDictionary<(Type HandlerType, Type EventType), MethodInfo> MethodCache = new();

    public BackgroundEventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        if (domainEvents is null) throw new ArgumentNullException(nameof(domainEvents));
        if (!domainEvents.Any()) return;

        foreach (var domainEvent in domainEvents)
        {
            if (domainEvent is null) continue;

            var eventType = domainEvent.GetType();
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            var handlers = _serviceProvider.GetServices(handlerType);

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
