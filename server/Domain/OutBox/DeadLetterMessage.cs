namespace Domain.OutBox
{
    /// <summary>
    /// Domain entity for dead-lettered messages stored in database.
    /// Separate from OutboxMessage to keep concerns isolated.
    /// </summary>
    public class DeadLetterMessage
    {
        public Guid Id { get; private set; }
        public string EventType { get; private set; } = null!;
        public string Payload { get; private set; } = null!;           // Original JSON payload
        public string Reason { get; private set; } = null!;            // Why it was dead-lettered
        public DateTimeOffset OccurredOnUtc { get; private set; }      // When original event occurred
        public DateTimeOffset DeadLetteredAtUtc { get; private set; }  // When moved to DLQ
        public bool IsReplayed { get; private set; }
        public DateTimeOffset? ReplayedAtUtc { get; private set; }
        public string? TraceId { get; private set; }                   // For distributed tracing

        // EF parameterless constructor
        private DeadLetterMessage() { }

        /// <summary>
        /// Creates new dead-letter message.
        /// 
        /// TAKES: eventType, payload, reason, occurredOnUtc, traceId?
        /// DOES: Initializes entity with Id = Guid.NewGuid(), DeadLetteredAtUtc = now, IsReplayed = false
        /// </summary>
        public DeadLetterMessage(string eventType, string payload, string reason, DateTimeOffset occurredOnUtc, string? traceId = null)
        {
            if (string.IsNullOrWhiteSpace(eventType)) throw new ArgumentException(nameof(eventType));
            if (string.IsNullOrWhiteSpace(payload)) throw new ArgumentException(nameof(payload));
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException(nameof(reason));

            Id = Guid.NewGuid();
            EventType = eventType;
            Payload = payload;
            Reason = reason;
            OccurredOnUtc = occurredOnUtc;
            DeadLetteredAtUtc = DateTimeOffset.UtcNow;
            TraceId = traceId;
            IsReplayed = false;
        }

        /// <summary>
        /// Marks message as replayed.
        /// TAKES: replayedAtUtc (DateTimeOffset)
        /// DOES: Sets IsReplayed = true, ReplayedAtUtc = replayedAtUtc
        /// </summary>
        public void MarkReplayed(DateTimeOffset replayedAtUtc)
        {
            IsReplayed = true;
            ReplayedAtUtc = replayedAtUtc;
        }
    }
}