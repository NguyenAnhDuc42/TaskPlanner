using System;
using System.Collections.Concurrent;
using System.Reflection;
using Application.Common;
using Application.Common.Interfaces;
using Application.EventHandlers;
using Application.EventHandlers.Interface;
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
    /// <summary>
    /// Dispatches event to all registered handlers and returns aggregated result.
    /// 
    /// TAKES: IIntegrationEvent (@event), CancellationToken
    /// DOES:
    ///   1. Starts Activity span for tracing: "DispatchEvent"
    ///   2. Resolve all IIntegrationEventHandler<TEvent> from IServiceProvider (DI)
    ///   3. Create List<IntegrationEventHandlingResult> results
    ///   4. FOREACH handler:
    ///      a. Start stopwatch
    ///      b. result = handler.HandleAsync(@event, ct)
    ///      c. IMetricsCollector.RecordHandlerDuration(handlerType, elapsed)
    ///      d. results.Add(result)
    ///   5. Aggregate results using priority rules:
    ///      - ANY result == DeadLetter → return DeadLetter (poison message, highest priority)
    ///      - ANY result == Retry → return Retry (needs backoff)
    ///      - ALL results == Skip → return Skip (no-op)
    ///      - OTHERWISE → return Success
    ///   6. Log aggregated result with handler breakdown (for debugging)
    /// RETURNS: Task<IntegrationEventHandlingResult> (aggregated decision)
    /// CONDITION: Called by KafkaConsumerWorker after deserializing message
    /// LOGIC: Ensures ANY handler requiring retry/DLQ overrides success from others.
    ///        On retry, ALL handlers re-invoked (idempotency required).
    /// </summary>
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
                var invokeResult = method.Invoke(handler, new object[] { @event, cancellationToken })!;
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
