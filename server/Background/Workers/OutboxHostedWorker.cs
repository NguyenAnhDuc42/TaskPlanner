using Microsoft.Extensions.Hosting;

namespace Background.Workers
{
    /// <summary>
    /// Background service that polls the outbox table and publishes pending messages to Kafka.
    /// Uses Postgres advisory lock to ensure only one worker publishes across multiple instances.
    /// </summary>
    public class OutboxHostedWorker : BackgroundService
    {
        /// <summary>
        /// Main execution loop for the hosted service.
        /// 
        /// TAKES: CancellationToken (stopping token from host)
        /// DOES:
        ///   1. Starts background RenewAdvisoryLockAsync task
        ///   2. Loops every OutboxConfig.PollDelaySeconds:
        ///      a. Calls AcquireAdvisoryLockAsync()
        ///      b. If lock acquired → ProcessBatchAsync()
        ///      c. If lock NOT acquired → skip iteration (another worker has lock)
        ///   3. On shutdown → ReleaseAdvisoryLockAsync() in finally block
        /// RETURNS: Task (void)
        /// CONDITION: Runs continuously until application shutdown
        /// </summary>
        protected override Task ExecuteAsync(CancellationToken stoppingToken) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Acquires Postgres advisory lock for single-publisher guarantee.
        /// 
        /// TAKES: CancellationToken
        /// DOES: Executes SELECT pg_try_advisory_lock(@advisoryLockKey) via Dapper/EF raw SQL
        /// RETURNS: Task<bool> (true if lock acquired, false if another worker holds it)
        /// CONDITION: Called at start of each polling iteration
        /// </summary>
        private Task<bool> AcquireAdvisoryLockAsync(CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Renews advisory lock to prevent timeout during long-running batches.
        /// 
        /// TAKES: CancellationToken
        /// DOES:
        ///   1. Runs in background loop (every 30 seconds)
        ///   2. Executes SELECT pg_advisory_lock(@advisoryLockKey) -- blocking renew
        /// RETURNS: Task (void, runs until cancellation)
        /// CONDITION: Started at beginning of ExecuteAsync, ensures lock not lost during processing
        /// </summary>
        private Task RenewAdvisoryLockAsync(CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Releases Postgres advisory lock on shutdown.
        /// 
        /// TAKES: CancellationToken
        /// DOES: Executes SELECT pg_advisory_unlock(@advisoryLockKey)
        /// RETURNS: Task (void)
        /// CONDITION: Called in finally block of ExecuteAsync on graceful shutdown
        /// </summary>
        private Task ReleaseAdvisoryLockAsync(CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Processes a batch of pending outbox messages.
        /// 
        /// TAKES: CancellationToken
        /// DOES:
        ///   1. IOutboxRepository.GetPendingWithLockAsync(batchSize)
        ///   2. IMetricsCollector.RecordOutboxBatchSize(count)
        ///   3. FOREACH message in batch:
        ///      a. Builds headers: "Event-Name", "Trace-Id", "Span-Id" (from Activity.Current)
        ///      b. Calls IIntegrationEventPublisher.PublishRawAsync(eventType, payload, routingKey, headers)
        ///      c. ON SUCCESS:
        ///         - IOutboxRepository.MarkSentAsync(id)
        ///         - IMetricsCollector.IncrementPublishSuccess(eventType)
        ///      d. ON FAILURE:
        ///         - IF attempts < MaxRetries:
        ///           * Compute backoff via BackoffCalculator.ComputeBackoff(attempts)
        ///           * IOutboxRepository.IncrementAttemptsAndRescheduleAsync(id, nextAvailableAt, backoff)
        ///         - IF attempts >= MaxRetries:
        ///           * IOutboxRepository.MarkDeadLetterAsync(id, reason)
        ///           * IMetricsCollector.IncrementPublishFailure(eventType, reason)
        /// RETURNS: Task<int> (number of messages processed)
        /// CONDITION: Called only when advisory lock is held
        /// </summary>
        private Task<int> ProcessBatchAsync(CancellationToken cancellationToken){
            throw new NotImplementedException();
        }
    }
}