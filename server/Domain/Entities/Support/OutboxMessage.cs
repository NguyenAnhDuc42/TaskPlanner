using System;
using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Support;

public class OutboxMessage : Composite
{
    public string Type { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public DateTimeOffset OccurredOnUtc { get; private set; }
    public DateTimeOffset? ProcessedOnUtc { get; private set; }
    public string? Error { get; private set; }
    public OutboxState State { get; private set; }

    private OutboxMessage() { } // EF

    public OutboxMessage(string type, string content, DateTimeOffset occurredOnUtc)
    {
        Id = Guid.NewGuid();
        Type = type;
        Content = content;
        OccurredOnUtc = occurredOnUtc;
        State = OutboxState.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsProcessed()
    {
        ProcessedOnUtc = DateTimeOffset.UtcNow;
        State = OutboxState.Sent;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsFailed(string error)
    {
        Error = error;
        State = OutboxState.DeadLetter;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
