using System;
using Infrastructure.Interfaces;

namespace Infrastructure.Events.IntergrationsEvents;


/// <summary>
/// Dead letter sink that publishes failed messages to DLQ Kafka topics 
/// and persists to database for replay.
/// </summary>
public class TopicDeadLetterSink : IDeadLetterSink
{
    /// <summary>
    /// Saves dead-lettered message to both Kafka DLQ topic and database.
    /// 
    /// TAKES: payload (string JSON), eventType (string), reason (string), CancellationToken
    /// DOES:
    ///   1. Construct dlqTopic = $"{eventType}-dlq"
    ///   2. Build headers:
    ///      - "Dead-Letter-Reason" → reason
    ///      - "Dead-Letter-Timestamp" → DateTimeOffset.UtcNow.ToString("O")
    ///      - Preserve: "Trace-Id" (from Activity.Current)
    ///   3. IProducer.ProduceAsync(dlqTopic, payload with headers) -- Kafka DLQ topic
    ///   4. IDeadLetterRepository.SaveAsync(new DeadLetterMessage {
    ///        Payload = payload,
    ///        EventType = eventType,
    ///        Reason = reason,
    ///        OccurredOnUtc = DateTimeOffset.UtcNow
    ///      }) -- Database persistence for replay API
    ///   5. IMetricsCollector.IncrementDeadLetterSaved(eventType)
    /// RETURNS: Task (void)
    /// CONDITION: Called when handler returns DeadLetter or max retries exceeded
    /// DUAL PERSISTENCE: Kafka topic for ops visibility + DB for programmatic replay
    /// </summary>
    public async Task SaveAsync(string payload, string? eventType, string reason, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
