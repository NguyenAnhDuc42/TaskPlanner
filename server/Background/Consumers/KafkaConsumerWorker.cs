using System;
using System.Text;
using System.Text.Json;
using Application.Common.Interfaces;
using Application.EventHandlers;
using Application.Interfaces.IntergrationEvent;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Background.Consumers;

public class KafkaConsumerWorker : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<KafkaConsumerWorker> _logger;
    private readonly IIntegrationEventDispatcher _dispatcher;
    private readonly RetryPolicy _retryPolicy;
    private readonly RetryOptions _retryOptions;
    private readonly IntegrationEventOptions _integrationOptions;
    public KafkaConsumerWorker(IConsumer<string, string> consumer, ILogger<KafkaConsumerWorker> logger, IIntegrationEventDispatcher dispatcher, RetryPolicy retryPolicy, IOptions<RetryOptions> retryOptions, IOptions<IntegrationEventOptions> integrationOptions)
    {
        _consumer = consumer;
        _logger = logger;
        _dispatcher = dispatcher;
        _retryPolicy = retryPolicy;
        _retryOptions = retryOptions.Value;
        _integrationOptions = integrationOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var topics = _integrationOptions.Topics.Values.ToArray();
        _consumer.Subscribe(topics);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer.Consume(TimeSpan.FromMilliseconds(100));
                if (consumeResult?.Message != null)
                {
                    var finalResult = await ProcessMessageWithRetry(consumeResult, stoppingToken);

                    // Handle the final result
                    HandleFinalResult(consumeResult, finalResult);
                }
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Error consuming from Kafka");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task<IntegrationEventHandlingResult> ProcessMessageWithRetry(ConsumeResult<string, string> consumeResult, CancellationToken cancellationToken)
    {
        var delay = _retryPolicy.InitialDelay;
        for (int attempt = 0; attempt <= _retryPolicy.MaxRetries; attempt++)
        {
            try
            {
                var eventTypeHeader = consumeResult.Message.Headers?.FirstOrDefault(h => h.Key == "eventType");
                if (eventTypeHeader == null)
                {
                    _logger.LogWarning("Message missing eventType header");
                    return IntegrationEventHandlingResult.DeadLetter;
                }
                var eventTypeName = Encoding.UTF8.GetString(eventTypeHeader.GetValueBytes());
                var eventType = Type.GetType(eventTypeName);
                if (eventType == null)
                {
                    _logger.LogWarning("Unknown event type: {EventType}", eventTypeName);
                    return IntegrationEventHandlingResult.Skip;
                }

                var @event = (IIntegrationEvent)JsonSerializer.Deserialize(consumeResult.Message.Value, eventType);


                var result = await _dispatcher.DispatchAsync(@event, cancellationToken);
                if (result == IntegrationEventHandlingResult.Success)
                {
                    _logger.LogDebug("Message processed successfully on attempt {Attempt}", attempt + 1);
                    return result;
                }
                if (result != IntegrationEventHandlingResult.Retry || attempt == _retryPolicy.MaxRetries)
                {
                    return result; // DeadLetter, Skip, or final retry
                }
                _logger.LogInformation("Retrying message in {Delay}. Attempt {Attempt}/{MaxRetries}",
                delay, attempt + 1, _retryPolicy.MaxRetries);
                await Task.Delay(delay, cancellationToken);

                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * _retryPolicy.BackoffMultiplier);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message on attempt {Attempt}", attempt + 1);
                if (attempt == _retryPolicy.MaxRetries) return IntegrationEventHandlingResult.DeadLetter;
            }
        }
        return IntegrationEventHandlingResult.DeadLetter;
    }

    private void HandleFinalResult(ConsumeResult<string, string> consumeResult, IntegrationEventHandlingResult finalResult)
    {
        switch (finalResult)
        {
            case IntegrationEventHandlingResult.Success:
                _logger.LogDebug("Message processed successfully. Topic: {Topic}, Offset: {Offset}",
                consumeResult.Topic, consumeResult.Offset);
                _consumer.Commit(consumeResult); // Commit the offset
                break;
            case IntegrationEventHandlingResult.Skip:
                _logger.LogInformation("Message skipped. Topic: {Topic}, Offset: {Offset}",
                consumeResult.Topic, consumeResult.Offset);
                _consumer.Commit(consumeResult); // Commit to move past this message
                break;
            case IntegrationEventHandlingResult.DeadLetter:
                _logger.LogError("Message sent to dead letter after all retries. Topic: {Topic}, Offset: {Offset}",
                consumeResult.Topic, consumeResult.Offset);

                // TODO: Send to dead letter queue/table
                HandleDeadLetter(consumeResult);
                _consumer.Commit(consumeResult); // Commit so we don't reprocess
                break;
            case IntegrationEventHandlingResult.Retry:
                // This shouldn't happen here, but just in case
                _logger.LogWarning("Unexpected retry result in final handling. Treating as dead letter.");
                HandleDeadLetter(consumeResult);
                _consumer.Commit(consumeResult);
                break;
        }
    }

    private void HandleDeadLetter(ConsumeResult<string, string> consumeResult)
    {
        // maybe in future
        // - Save to a database table
        // - Publish to a dead letter topic
        // - Send to a monitoring system
        _logger.LogError("DEAD LETTER: Topic={Topic}, Partition={Partition}, Offset={Offset}, Value={Value}",
        consumeResult.Topic,
        consumeResult.Partition,
        consumeResult.Offset,
        consumeResult.Message.Value);
    }
}
