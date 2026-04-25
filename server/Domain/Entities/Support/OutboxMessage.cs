using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class OutboxMessage : Entity
{
    public string Type { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public DateTimeOffset OccurredOnUtc { get; private set; }
    public DateTimeOffset? ProcessedOnUtc { get; private set; }
    public string? Error { get; private set; }
    public OutboxState State { get; private set; }
    public int ErrorCount { get; private set; }
    public DateTimeOffset? ScheduledAtUtc { get; private set; }

    private OutboxMessage() { } // EF

    public OutboxMessage(string type, string content, DateTimeOffset occurredOnUtc)
    {
        Id = Guid.NewGuid();
        Type = type;
        Content = content;
        OccurredOnUtc = occurredOnUtc;
        State = OutboxState.Pending;
        ErrorCount = 0;
    }

    public void MarkAsProcessed()
    {
        ProcessedOnUtc = DateTimeOffset.UtcNow;
        State = OutboxState.Sent;
        UpdateTimestamp();
    }

    public void MarkAsFailed(string error)
    {
        Error = error;
        ErrorCount++;

        if (ErrorCount >= 5) 
        {
            State = OutboxState.DeadLetter;
        }
        else
        {
            var delaySeconds = ErrorCount switch
            {
                1 => 10,
                2 => 60,
                3 => 300,
                _ => 900
            };
            ScheduledAtUtc = DateTimeOffset.UtcNow.AddSeconds(delaySeconds);
            State = OutboxState.Pending;
        }
        
        UpdateTimestamp();
    }
}
