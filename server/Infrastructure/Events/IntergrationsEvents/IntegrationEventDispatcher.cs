using System;
using System.Collections.Concurrent;
using System.Reflection;
using Application.Common;
using Application.Common.Interfaces;
using Application.EventHandlers;
using Application.Interfaces.IntergrationEvent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Events.IntergrationsEvents;

public class IntegrationEventDispatcher : IIntegrationEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> _methodCache = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IntegrationEventDispatcher> _logger;
    public IntegrationEventDispatcher(IServiceProvider serviceProvider, ILogger<IntegrationEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

    }
    public async Task<IntegrationEventHandlingResult> DispatchAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        var eventType = @event.GetType();
        var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
        var handlers = _serviceProvider.GetServices(handlerType);
        if (!handlers.Any())
        {
            _logger.LogWarning("No handlers found for event type {EventType}", eventType.Name);
            return IntegrationEventHandlingResult.Skip;
        }

        var method = _methodCache.GetOrAdd(handlerType, t => t.GetMethod("HandleAsync")!);
        var results = new List<IntegrationEventHandlingResult>();
        foreach (var handler in handlers)
        {
            try
            {
                var invokeResult = method.Invoke(handler, [@event, cancellationToken])!;
                var task = (Task<IntegrationEventHandlingResult>)invokeResult;
                var result = await task;
                results.Add(result);

                _logger.LogDebug("Handler {HandlerType} returned {Result} for event {EventType}",
                    handler.GetType().Name, result, eventType.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handler {HandlerType} threw exception for event {EventType}",
                   handler.GetType().Name, eventType.Name);
                results.Add(IntegrationEventHandlingResult.DeadLetter);
            }

        }
        return DetermineOverallResult(results);
    }
    private IntegrationEventHandlingResult DetermineOverallResult(List<IntegrationEventHandlingResult> results)
    {
        if (!results.Any())
            return IntegrationEventHandlingResult.Skip;

        // Priority: DeadLetter > Retry > Skip > Success
        if (results.Any(r => r == IntegrationEventHandlingResult.DeadLetter))
            return IntegrationEventHandlingResult.DeadLetter;

        if (results.Any(r => r == IntegrationEventHandlingResult.Retry))
            return IntegrationEventHandlingResult.Retry;

        if (results.Any(r => r == IntegrationEventHandlingResult.Skip))
            return IntegrationEventHandlingResult.Skip;

        return IntegrationEventHandlingResult.Success;
    }
}
