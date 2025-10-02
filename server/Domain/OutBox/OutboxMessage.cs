using System;
using Domain.Enums;

namespace Domain.OutBox
{

    public class OutboxMessage
    {
        public Guid Id { get; private set; }
        public DateTimeOffset OccurredOnUtc { get; private set; }
        public string EventType { get; private set; } = null!;         // public event name, e.g. "UserRegisteredEvent"
        public string Payload { get; private set; } = null!;           // JSON payload
        public string? RoutingKey { get; private set; }                // optional topic override
        public string? DeduplicationKey { get; private set; }
        public string? CreatedBy { get; private set; }

        public int Attempts { get; private set; }
        public DateTimeOffset AvailableAtUtc { get; private set; }     // when this message becomes available for publish
        public DateTimeOffset? SentOnUtc { get; private set; }         // when published successfully
        public DateTimeOffset? ProcessedOnUtc { get; private set; }    // when marked processed (sent/dead)
        public string? LastError { get; private set; }
        public OutboxState State { get; private set; }

        // parameterless ctor for EF / serialization
        private OutboxMessage() { }

        public OutboxMessage(
            string eventType,
            string payload,
            string? routingKey = null,
            string? deduplicationKey = null,
            string? createdBy = null)
        {
            if (string.IsNullOrWhiteSpace(eventType)) throw new ArgumentException("eventType must be provided", nameof(eventType));
            if (string.IsNullOrWhiteSpace(payload)) throw new ArgumentException("payload must be provided", nameof(payload));

            Id = Guid.NewGuid();
            OccurredOnUtc = DateTimeOffset.UtcNow;
            EventType = eventType;
            Payload = payload;
            RoutingKey = routingKey;
            DeduplicationKey = deduplicationKey;
            CreatedBy = createdBy;
            Attempts = 0;
            AvailableAtUtc = DateTimeOffset.UtcNow;
            State = OutboxState.Pending;
        }

        /// <summary>
        /// Mark message as successfully sent.
        /// </summary>
        public void MarkSent()
        {
            State = OutboxState.Sent;
            SentOnUtc = DateTimeOffset.UtcNow;
            ProcessedOnUtc = SentOnUtc;
            LastError = null;
        }

        /// <summary>
        /// Increment attempts and set next available time using the supplied backoff.
        /// </summary>
        public void IncrementAttemptsAndReschedule(TimeSpan backoff, DateTimeOffset nextAvailableAt)
        {
            if (backoff < TimeSpan.Zero) backoff = TimeSpan.Zero;
            Attempts += 1;
            AvailableAtUtc = nextAvailableAt;
            LastError = null; // optional: clear last transient error on reschedule
            State = OutboxState.Pending;
        }

        /// <summary>
        /// Mark message as dead-lettered with a reason.
        /// </summary>
        public void MarkDeadLetter(string reason)
        {
            State = OutboxState.DeadLetter;
            LastError = reason ?? string.Empty;
            ProcessedOnUtc = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Record an error without changing state (useful for logging/transient failures).
        /// </summary>
        public void SetError(string error)
        {
            LastError = error;
        }

        /// <summary>
        /// Helper to determine if the message has been processed (Sent or DeadLetter).
        /// </summary>
        public bool IsProcessed => State == OutboxState.Sent || State == OutboxState.DeadLetter;
    }
}
