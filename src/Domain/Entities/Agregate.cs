using System;
using src.Domain.Event;

namespace src.Domain.Entities;

public abstract class Aggregate<TId> : Entity<TId> where TId : notnull
{
     private readonly List<DomainEvent> _domainEvents = new List<DomainEvent>();
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected Aggregate() : base() { }
    protected Aggregate(TId id) : base(id) { }

    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        if (domainEvent == null) throw new ArgumentNullException(nameof(domainEvent));
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvent()
    {
        _domainEvents.Clear();
    }
}
