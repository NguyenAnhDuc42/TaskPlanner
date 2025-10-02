using System;

namespace Application.EventHandlers.Interface;


/// <summary>
/// Service for enqueueing integration events to the outbox within a transaction.
/// Must be called inside an active UnitOfWork/DbContext transaction.
/// </summary>
public interface IOutboxService
{
    /// <summary>
    /// Enqueues a single event to the outbox.
    ///
    /// TAKES:
    ///   - eventType (string): public event name (e.g. "UserRegisteredEvent").
    ///   - payloadJson (string): serialized JSON payload.
    ///   - routingKey (string?): optional topic override.
    ///   - deduplicationKey (string?): optional idempotency key.
    ///   - createdBy (string?): audit user id/name.
    ///   - cancellationToken (CancellationToken)
    ///
    /// DOES:
    ///   1. Validate eventType and payloadJson not empty.
    ///   2. If deduplicationKey provided => call IOutboxRepository.ExistsAsync(deduplicationKey).
    ///      - If true => throw DuplicateEventException.
    ///   3. Create OutboxMessage via domain factory (OutboxMessage.CreatePending(...)).
    ///   4. Call IOutboxRepository.SaveAsync(message) (adds to DbContext; does NOT SaveChanges).
    ///
    /// RETURNS: Task (void)
    ///
    /// CONDITION:
    ///   - Caller must operate inside active DbContext transaction / UnitOfWork and commit later.
    ///
    /// USES:
    ///   - IOutboxRepository.ExistsAsync
    ///   - IOutboxRepository.SaveAsync
    ///
    /// PRECONDITIONS:
    ///   - DbContext is available in the same scope.
    ///
    /// POSTCONDITIONS:
    ///   - OutboxMessage is tracked by DbContext and will be persisted at Commit/SaveChanges.
    ///
    /// ERRORS:
    ///   - Throws DuplicateEventException on dedupe collision.
    ///   - Throws ArgumentException for invalid inputs.
    /// </summary>
    Task EnqueueAsync(string eventType, string payloadJson, string? routingKey = null, string? deduplicationKey = null, string? createdBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enqueues multiple events in a single batch operation.
    ///
    /// TAKES:
    ///   - events (IEnumerable of tuples): (eventType, payloadJson, routingKey?, deduplicationKey?, createdBy?)
    ///   - cancellationToken
    ///
    /// DOES:
    ///   1. Validate input collection not empty.
    ///   2. Aggregate deduplication keys and call a batch Exists check via IOutboxRepository (prefer single DB query).
    ///      - If any deduplication key exists => throw DuplicateEventException listing offending keys.
    ///   3. Create OutboxMessage entities for all events using domain factory.
    ///   4. Call IOutboxRepository.SaveBatchAsync(messages) (adds to DbContext).
    ///
    /// RETURNS: Task (void)
    ///
    /// CONDITION:
    ///   - Must be executed inside an ambient transaction.
    ///
    /// USES:
    ///   - IOutboxRepository.ExistsAsync (batch optimized) or per-key ExistsAsync
    ///   - IOutboxRepository.SaveBatchAsync
    ///
    /// IMPLEMENTATION NOTES:
    ///   - Prefer one SQL EXISTS ... IN (@keys) call to verify duplicates.
    ///   - If DB unique index exists, be prepared to catch unique-constraint exceptions at SaveChanges and map to DuplicateEventException.
    /// </summary>
    Task EnqueueBatchAsync(IEnumerable<(string eventType, string payloadJson, string? routingKey, string? deduplicationKey, string? createdBy)> events, CancellationToken cancellationToken = default);
}

