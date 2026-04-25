using System.Collections.Concurrent;
using System.Linq.Expressions;
using Application.Common.Interfaces;
using Background.Interfaces;
using Domain.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Services.Background;

public class BackgroundEventDispatcher(IServiceProvider serviceProvider) : IBackgroundEventDispatcher
{
    private static readonly ConcurrentDictionary<(Type HandlerType, Type EventType), Func<object, object, CancellationToken, Task>> DelegateCache = new();
    private static readonly ConcurrentDictionary<Type, Type> GenericHandlerTypeCache = new();

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        if (domainEvents is null) throw new ArgumentNullException(nameof(domainEvents));
        
        foreach (var domainEvent in domainEvents)
        {
            if (domainEvent is null) continue;

            var eventType = domainEvent.GetType();
            
            var handlerType = GenericHandlerTypeCache.GetOrAdd(eventType, 
                t => typeof(IDomainEventHandler<>).MakeGenericType(t));
            
            var handlers = serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                if (handler == null) continue;

                var action = DelegateCache.GetOrAdd((handler.GetType(), eventType), key => 
                    CompileHandlerDelegate(key.HandlerType, key.EventType));

                await action(handler, domainEvent, cancellationToken);
            }
        }
    }

    private static Func<object, object, CancellationToken, Task> CompileHandlerDelegate(Type handlerType, Type eventType)
    {
        var handlerParam = Expression.Parameter(typeof(object), "handler");
        var eventParam = Expression.Parameter(typeof(object), "event");
        var tokenParam = Expression.Parameter(typeof(CancellationToken), "token");

        var method = handlerType.GetMethod("Handle", [eventType, typeof(CancellationToken)])
                     ?? throw new InvalidOperationException($"Handle method not found on {handlerType.Name}");

        var call = Expression.Call(
            Expression.Convert(handlerParam, handlerType),
            method,
            Expression.Convert(eventParam, eventType),
            tokenParam
        );

        return Expression.Lambda<Func<object, object, CancellationToken, Task>>(call, handlerParam, eventParam, tokenParam).Compile();
    }
}