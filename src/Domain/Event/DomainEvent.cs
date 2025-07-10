using System;

namespace src.Domain.Event;

public abstract class DomainEvent
{
    string EventId { get; } = null!;
    DateTimeOffset OccuredOn { get; }
    public DomainEvent()
    {
        EventId = Guid.NewGuid().ToString();
        OccuredOn = DateTimeOffset.UtcNow;
    }
 
}
