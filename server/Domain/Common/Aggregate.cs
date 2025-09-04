using Domain.Common;
using Domain.Common.Interfaces;

public abstract class Aggregate : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();
    
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public bool HasDomainEvents => _domainEvents.Count > 0;

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
        UpdateTimestamp();
    }

    protected void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Remove(domainEvent);
        UpdateTimestamp();
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}