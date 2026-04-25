using Background.Interfaces;
using Background.Services;
using Domain.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Domain.Events;

namespace Background.Jobs;

public class ProcessOutboxJob(
    IBackgroundOutboxAccessor outboxAccessor,
    IBackgroundEventDispatcher domainDispatcher,
    DomainEventTypeProvider typeProvider,
    ILogger<ProcessOutboxJob> logger)
{
    public async Task<bool> RunAsync()
    {
        var messages = await outboxAccessor.GetPendingMessagesAsync(50);

        if (!messages.Any()) return false;

        foreach (var message in messages)
        {
            try
            {
                var eventType = typeProvider.GetEventType(message.Type);
                if (eventType == null)
                {
                    logger.LogWarning("Could not find type mapping for: {Type}. Skipping message {Id}", message.Type, message.Id);
                    message.MarkAsFailed($"Could not find type mapping for: {message.Type}");
                    continue;
                }

                var domainEvent = JsonSerializer.Deserialize(message.Content, eventType) as IDomainEvent;
                if (domainEvent == null)
                {
                    logger.LogError("Could not deserialize content for message {Id} of type {Type}", message.Id, message.Type);
                    message.MarkAsFailed($"Could not deserialize content for: {message.Type}");
                    continue;
                }

                logger.LogTrace("Processing {EventType} message {Id}", message.Type, message.Id);
                
                await domainDispatcher.DispatchAsync([domainEvent]);
                
                message.MarkAsProcessed();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox message {Id} ({Type})", message.Id, message.Type);
                outboxAccessor.ResetTracking(message); // Ensure EF doesn't keep tracking the entity as modified
                message.MarkAsFailed(ex.Message);
            }
        }

        await outboxAccessor.SaveChangesAsync();
        return true;
    }
}
