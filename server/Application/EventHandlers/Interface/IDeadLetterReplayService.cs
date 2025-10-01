using System;

namespace Application.EventHandlers.Interface;

/// <summary>
/// Service for replaying dead-lettered messages back into the normal event flow.
/// Used by admin endpoints or manual intervention scripts.
/// </summary>
public interface IDeadLetterReplayService
{
    /// <summary>
    /// Replays a single dead-letter message by ID.
    /// 
    /// TAKES: deadLetterId (Guid from DeadLetterMessage table), CancellationToken
    /// DOES:
    ///   1. message = IDeadLetterRepository.GetByIdAsync(deadLetterId)
    ///   2. IF message == null → throw NotFoundException
    ///   3. IIntegrationEventPublisher.PublishRawAsync(message.EventType, message.Payload)
    ///   4. IDeadLetterRepository.MarkReplayedAsync(deadLetterId, DateTimeOffset.UtcNow)
    /// RETURNS: Task (void)
    /// CONDITION: Called by admin API endpoint or script after fixing underlying issue
    /// </summary>
    Task ReplayAsync(Guid deadLetterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replays multiple dead-letter messages matching query criteria.
    /// 
    /// TAKES: DeadLetterQuery (filter: eventType, dateRange, reason), CancellationToken
    /// DOES:
    ///   1. messages = IDeadLetterRepository.QueryAsync(query, limit: 100)
    ///   2. FOREACH message with rate limiting (e.g., 10 msgs/sec):
    ///      a. IIntegrationEventPublisher.PublishRawAsync(eventType, payload)
    ///      b. IDeadLetterRepository.MarkReplayedAsync(message.Id)
    ///   3. Return summary: total replayed, failed
    /// RETURNS: Task<ReplayResult> (count, errors)
    /// CONDITION: Batch replay for recovering from systemic failures
    /// </summary>
    Task<ReplayResult> ReplayBatchAsync(DeadLetterQuery query, CancellationToken cancellationToken = default);
}
