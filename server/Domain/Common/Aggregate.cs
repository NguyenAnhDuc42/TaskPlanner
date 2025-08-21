using System;
using System.Collections.Concurrent;
using Domain.Common.Interfaces;

namespace Domain.Common;

public class Aggregate : Entity
{
    private readonly ConcurrentQueue<IDomainEvent> _domainEvents = new();
    
    public bool HasDomainEvents => !_domainEvents.IsEmpty;
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.ToArray();
    
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Enqueue(domainEvent);
        UpdateTimestamp();
    }
    
    public void ClearDomainEvents()
    {
        while (_domainEvents.TryDequeue(out _)) { }
    }
}

