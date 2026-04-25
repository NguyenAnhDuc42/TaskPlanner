using Background.Interfaces;
using Domain.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Domain.Events;

namespace Background.Jobs;

public class ProcessOutboxJob
{
    private readonly IBackgroundOutboxAccessor _outboxAccessor;
    private readonly IBackgroundEventDispatcher _domainDispatcher;
    private readonly ILogger<ProcessOutboxJob> _logger;

    public ProcessOutboxJob(
        IBackgroundOutboxAccessor outboxAccessor,
        IBackgroundEventDispatcher domainDispatcher,
        ILogger<ProcessOutboxJob> logger)
    {
        _outboxAccessor = outboxAccessor;
        _domainDispatcher = domainDispatcher;
        _logger = logger;
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Type?> TypeCache = new();
    private static readonly object TypeLock = new();
    private static bool _typesLoaded = false;

    private static void LoadTypes()
    {
        if (_typesLoaded) return;
        lock (TypeLock)
        {
            if (_typesLoaded) return;
            var eventTypes = typeof(BaseDomainEvent).Assembly.GetTypes()
                .Where(t => typeof(IDomainEvent).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
            
            foreach (var type in eventTypes)
            {
                TypeCache.TryAdd(type.Name, type);
            }
            _typesLoaded = true;
        }
    }

    public async Task RunAsync()
    {
        LoadTypes();

        var messages = await _outboxAccessor.GetPendingMessagesAsync(50);

        if (!messages.Any()) return;

        foreach (var message in messages)
        {
            try
            {
                if (!TypeCache.TryGetValue(message.Type, out var eventType) || eventType == null)
                {
                    message.MarkAsFailed($"Could not find type mapping for: {message.Type}");
                    continue;
                }

                var domainEvent = JsonSerializer.Deserialize(message.Content, eventType) as IDomainEvent;
                if (domainEvent == null)
                {
                    message.MarkAsFailed($"Could not deserialize content for: {message.Type}");
                    continue;
                }

                _logger.LogTrace("Processing {EventType} message {Id}", message.Type, message.Id);
                await _domainDispatcher.DispatchAsync(new[] { domainEvent });
                message.MarkAsProcessed();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox message {Id}", message.Id);
                _outboxAccessor.ResetTracking(message);
                message.MarkAsFailed(ex.Message);
            }
        }

        await _outboxAccessor.SaveChangesAsync();
    }
}
