using Domain.OutBox;

namespace Application.Interfaces
{
    /// <summary>
    /// Repository for outbox message persistence and retrieval.
    /// All methods participate in ambient DbContext transaction / connection when required.
    /// </summary>
    public interface IOutboxRepository
    {
        /// <summary>
        /// Saves a single outbox message to the database.
        ///
        /// TAKES: OutboxMessage entity, CancellationToken
        ///
        /// DOES:
        ///   - Adds entity to the DbContext (ChangeTracker) but DOES NOT call SaveChanges.
        ///
        /// RETURNS: Task (void)
        ///
        /// CONDITION:
        ///   - Caller must commit transaction / SaveChanges later.
        ///
        /// USES:
        ///   - OutboxDbContext.OutboxMessages.Add(entity)
        /// </summary>
        Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves multiple outbox messages in a batch.
        ///
        /// TAKES: IEnumerable<OutboxMessage>, CancellationToken
        ///
        /// DOES:
        ///   - Adds all entities to DbContext in a single AddRange call.
        ///
        /// RETURNS: Task (void)
        ///
        /// CONDITION:
        ///   - Caller commits later.
        ///
        /// OPTIMIZATION:
        ///   - Consider chunking very large lists to avoid EF change tracker blowup.
        /// </summary>
        Task SaveBatchAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a deduplication key already exists for a pending message.
        ///
        /// TAKES: deduplicationKey (string), CancellationToken
        ///
        /// DOES:
        ///   - Executes SELECT EXISTS (...) query with filter: state = Pending AND deduplication_key = @key
        ///
        /// RETURNS: Task<bool> (true if duplicate exists)
        ///
        /// CONDITION:
        ///   - Called before EnqueueAsync to enforce idempotency.
        ///
        /// USES:
        ///   - OutboxDbContext or raw SQL.
        ///
        /// IMPLEMENTATION NOTES:
        ///   - Implement using an indexed query for speed. If multiple keys required, implement a batch Exists (IN) method for EnqueueBatchAsync.
        /// </summary>
        Task<bool> ExistsAsync(string deduplicationKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves pending messages that are ready for publishing with row-level locking.
        ///
        /// TAKES: batchSize (int), CancellationToken
        ///
        /// DOES:
        ///   1. Runs within an open DB transaction on the same DbContext/connection.
        ///   2. Executes:
        ///      SELECT * FROM outbox_messages
        ///      WHERE state = Pending AND available_at_utc <= NOW()
        ///      ORDER BY occurred_on_utc ASC
        ///      LIMIT @batchSize
        ///      FOR UPDATE SKIP LOCKED
        ///   3. Returns entities tracked by DbContext (so updates can be saved on the same transaction).
        ///
        /// RETURNS: Task<IReadOnlyList<OutboxMessage>>
        ///
        /// CONDITION:
        ///   - Caller must use the same DbContext instance to update these tracked entities.
        ///
        /// IMPLEMENTATION NOTES:
        ///   - Use FromSqlRaw or Dapper on the same connection/transaction; if using Dapper, re-attach / re-load via EF before updating, or perform updates via SQL.
        /// </summary>
        Task<IReadOnlyList<OutboxMessage>> GetPendingWithLockAsync(int batchSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a message as successfully sent.
        ///
        /// TAKES: id (Guid), CancellationToken
        ///
        /// DOES:
        ///   1. Loads tracked entity (preferably previously selected via FOR UPDATE on same transaction).
        ///   2. Calls domain method OutboxMessage.MarkSent() to set SentOnUtc/ProcessedOnUtc/State.
        ///   3. Calls DbContext.SaveChanges/SaveChangesAsync or leaves to caller (document which approach).
        ///
        /// RETURNS: Task (void)
        ///
        /// CONDITION:
        ///   - Should be called after successful IIntegrationEventPublisher.PublishRawAsync.
        ///   - If selection used Dapper and entity not tracked, either re-query via EF with the same transaction or update via SQL.
        /// </summary>
        Task MarkSentAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a message as dead-lettered with error reason.
        ///
        /// TAKES: id (Guid), reason (string), CancellationToken
        ///
        /// DOES:
        ///   1. Load entity by id.
        ///   2. Call OutboxMessage.MarkDeadLetter(reason).
        ///   3. Persist change (SaveChangesAsync or leave to caller).
        ///
        /// RETURNS: Task (void)
        ///
        /// CONDITION:
        ///   - Called when attempts >= MaxRetries or fatal publish error.
        ///   - Also create DeadLetterMessage record (via IDeadLetterRepository) if DLQ DB persistence desired.
        /// </summary>
        Task MarkDeadLetterAsync(Guid id, string reason, CancellationToken cancellationToken = default);

        /// <summary>
        /// Increments attempt count and reschedules message for future processing.
        ///
        /// TAKES: id (Guid), nextAvailableAt (DateTimeOffset), backoff (TimeSpan), CancellationToken
        ///
        /// DOES:
        ///   1. Load entity.
        ///   2. Call OutboxMessage.IncrementAttemptsAndReschedule(backoff, nextAvailableAt).
        ///   3. Persist change.
        ///
        /// RETURNS: Task (void)
        ///
        /// CONDITION:
        ///   - Called when publish fails and Attempts < MaxRetries.
        /// </summary>
        Task IncrementAttemptsAndRescheduleAsync(Guid id, DateTimeOffset nextAvailableAt, TimeSpan backoff, CancellationToken cancellationToken = default);
    }
}
