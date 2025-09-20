using System;

namespace Domain.OutBox;

public class OutboxMessage
{
    public Guid Id { get; private set; }
    public DateTimeOffset OccurredOn { get; private set; }
    public string Type { get; private set; } = null!;
    public string Payload { get; private set; } = null!; // JSON
    public string? RoutingKey { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset AvailableAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? LastError { get; private set; }
    public bool IsProcessed { get; private set; }
    public string? DeduplicationKey { get; private set; }
    public string? CreatedBy { get; private set; }

    private OutboxMessage() { }
    public OutboxMessage(string type, string payload, string? routingKey = null, string? deduplicationKey = null, string? createdBy = null)
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTimeOffset.UtcNow;
        Type = type;
        Payload = payload;
        RoutingKey = routingKey;
        DeduplicationKey = deduplicationKey;
        CreatedBy = createdBy;
        Attempts = 0;
        AvailableAt = DateTimeOffset.UtcNow;
        IsProcessed = false;
    }
    public void MarkProcessed() => (IsProcessed, ProcessedAt) = (true, DateTimeOffset.UtcNow);
    public void IncrementAttempts(TimeSpan backoff)
    {
        Attempts++;
        AvailableAt = DateTimeOffset.UtcNow.Add(backoff);
    }

    public void SetError(string error)
    {
        LastError = error;
    }

}
