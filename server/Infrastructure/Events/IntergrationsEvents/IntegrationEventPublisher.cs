using System;
using System.Text;
using System.Text.Json;
using Application.Interfaces.IntergrationEvent;
using Confluent.Kafka;
using Microsoft.Build.Framework;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Events.IntergrationsEvents;

public class IntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<IntegrationEventPublisher> _logger;
    public IntegrationEventPublisher(IProducer<string, string> producer, ILogger<IntegrationEventPublisher> logger)
    {
        _producer = producer;
        _logger = logger;
    }
    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        var type = @event.GetType();

        var assemblyQualifiedName = type.AssemblyQualifiedName;
        if (assemblyQualifiedName is null)
        {
            var errorMessage = $"Cannot publish event of type '{type.Name}'. The AssemblyQualifiedName is null, " +
                            "indicating it may be a generic type parameter which cannot be serialized robustly.";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }
        var topic = type.Name;
        var payload = JsonSerializer.Serialize(@event, type);

        var headers = new Headers
        {
            { "eventType", Encoding.UTF8.GetBytes(assemblyQualifiedName) }
        };
        try
        {
            var message = new Message<string, string> { Key = Guid.NewGuid().ToString(), Value = payload, Headers = headers };
            var result = await _producer.ProduceAsync(topic, message, cancellationToken);
            _logger.LogInformation("Published {EventType} to Kafka topic {Topic}, offset {Offset}",
                @event.GetType().Name, topic, result);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish {EventType} to Kafka", @event.GetType().Name);
            throw;
        }
    }
}
