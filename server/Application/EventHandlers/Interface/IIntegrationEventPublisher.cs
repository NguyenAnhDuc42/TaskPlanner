using System;
using Application.Common.Interfaces;
using Confluent.Kafka;

namespace Application.EventHandlers.Interface;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default);
    Task PublishRawAsync(string eventType, string payloadJson, string? routingKey = null, Headers? headers = null, CancellationToken cancellationToken = default);

}
