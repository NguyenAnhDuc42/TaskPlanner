using Domain.OutBox;

namespace Application.Interfaces
{
    /// <summary>
    /// Repository for outbox message persistence and retrieval.
    /// All methods participate in ambient DbContext transaction.
    /// </summary>
    public interface IOutboxRepository
    {
        /// <summary>
        /// Saves a single outbox message to the database.
        /// 
        /// TAKES: OutboxMessage entity, CancellationToken
        /// DOES: Adds entity to DbContext (does NOT call SaveChanges)
        /// RETURNS: Task (void)
        /// CONDITION: Called within active transaction
        /// </summary>
        Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves multiple outbox messages in a batch.
        /// 
        /// TAKES: Collection of OutboxMessage entities, CancellationToken
        /// DOES: Adds all entities to DbContext in single operation
        /// RETURNS: Task (void)
        /// CONDITION: Called within active transaction
        /// </summary>
        Task SaveBatchAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a deduplication key already exists for a pending message.
        /// 
        /// TAKES: deduplicationKey (string), CancellationToken
        /// DOES: Executes SELECT EXISTS query with filter: state = Pending AND deduplication_key = @key
        /// RETURNS: Task<bool> (true if duplicate exists)
        /// CONDITION: Called before EnqueueAsync to enforce idempotency
        /// </summary>
        Task<bool> ExistsAsync(string deduplicationKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves pending messages that are ready for publishing with row-level locking.
        /// 
        /// TAKES: batchSize (int), CancellationToken
        /// DOES: 
        ///   1. SELECT ... FROM outbox WHERE state = Pending AND available_at_utc <= NOW()
        ///   2. ORDER BY occurred_on_utc ASC
        ///   3. LIMIT @batchSize
        ///   4. FOR UPDATE SKIP LOCKED (Postgres row-level lock, prevents multiple workers from grabbing same rows)
        /// RETURNS: Task<IReadOnlyList<OutboxMessage>>
        /// CONDITION: Called by OutboxHostedWorker after acquiring advisory lock
        /// </summary>
        Task<IReadOnlyList<OutboxMessage>> GetPendingWithLockAsync(int batchSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a message as successfully sent.
        /// 
        /// TAKES: OutboxMessage id (Guid), CancellationToken
        /// DOES:
        ///   1. Loads entity by Id
        ///   2. Calls OutboxMessage.MarkSent()
        ///   3. Commits changes to DB
        /// RETURNS: Task (void)
        /// CONDITION: Called after IIntegrationEventPublisher.PublishRawAsync succeeds
        /// </summary>
        Task MarkSentAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a message as dead-lettered with error reason.
        /// 
        /// TAKES: OutboxMessage id (Guid), reason (string), CancellationToken
        /// DOES:
        ///   1. Loads entity by Id
        ///   2. Calls OutboxMessage.MarkDeadLetter(reason)
        ///   3. Commits changes to DB
        /// RETURNS: Task (void)
        /// CONDITION: Called when attempts >= MaxRetries or immediate DeadLetter decision
        /// </summary>
        Task MarkDeadLetterAsync(Guid id, string reason, CancellationToken cancellationToken = default);

        /// <summary>
        /// Increments retry attempts and reschedules message for future processing.
        /// 
        /// TAKES: OutboxMessage id (Guid), nextAvailableAt (DateTimeOffset), backoff (TimeSpan), CancellationToken
        /// DOES:
        ///   1. Loads entity by Id
        ///   2. Calls OutboxMessage.IncrementAttemptsAndReschedule(backoff)
        ///   3. Sets AvailableAtUtc = nextAvailableAt
        ///   4. Commits changes to DB
        /// RETURNS: Task (void)
        /// CONDITION: Called when publish fails and attempts < MaxRetries
        /// </summary>
        Task IncrementAttemptsAndRescheduleAsync(Guid id, DateTimeOffset nextAvailableAt, TimeSpan backoff, CancellationToken cancellationToken = default);
    }
}