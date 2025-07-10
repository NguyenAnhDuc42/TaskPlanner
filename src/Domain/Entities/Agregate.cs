using System;
using src.Domain.Event;

namespace src.Domain.Entities;

public abstract class Agregate<TId> : Entity<TId> where TId : struct
{
     private readonly List<DomainEvent> _domainEvents = new List<DomainEvent>();
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected Agregate() : base() { }
    protected Agregate(TId id) : base(id) { }

    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        if (domainEvent == null) throw new ArgumentNullException(nameof(domainEvent));
        _domainEvents.Add(domainEvent);
    }
    public IReadOnlyCollection<DomainEvent> GetDomainEvents()
    {
        return _domainEvents.AsReadOnly();
    }

    public void ClearDomainEvent()
    {
        _domainEvents.Clear();
    }
}
