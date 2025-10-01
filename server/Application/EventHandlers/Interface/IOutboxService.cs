using System;

namespace Application.EventHandlers.Interface;

public interface IOutboxService
{
    /// <summary>
    /// Enqueues a single event to the outbox.
    /// 
    /// TAKES: eventType (public name), payloadJson (serialized event), optional routingKey (topic override), 
    ///        optional deduplicationKey (idempotency), optional createdBy (audit), CancellationToken
    /// DOES: 
    ///   1. Checks if deduplicationKey exists via IOutboxRepository.ExistsAsync
    ///   2. Throws DuplicateEventException if duplicate found
    ///   3. Creates OutboxMessage entity
    ///   4. Calls IOutboxRepository.SaveAsync (adds to DbContext, does NOT commit)
    /// RETURNS: Task (void)
    /// CONDITION: Must be called inside active transaction, caller commits transaction later
    /// </summary>
    Task EnqueueAsync(string eventType, string payloadJson, string? routingKey = null, string? deduplicationKey = null, string? createdBy = null, CancellationToken ct = default);
    /// <summary>
    /// Enqueues multiple events in a single batch operation.
    /// 
    /// TAKES: Collection of (eventType, payloadJson, routingKey?, deduplicationKey?, createdBy?), CancellationToken
    /// DOES:
    ///   1. Validates all deduplicationKeys in batch via single DB query
    ///   2. Throws DuplicateEventException if any duplicates found
    ///   3. Creates OutboxMessage entities for all events
    ///   4. Calls IOutboxRepository.SaveBatchAsync (single DB round-trip)
    /// RETURNS: Task (void)
    /// CONDITION: Called inside active transaction for bulk operations
    /// </summary>
    Task EnqueueBatchAsync(IEnumerable<(string eventType, string payloadJson, string? routingKey, string? deduplicationKey, string? createdBy)> events, CancellationToken cancellationToken = default);
}