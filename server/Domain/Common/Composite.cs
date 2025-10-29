
using Domain.Common.Interfaces;

namespace Domain.Common;

public abstract class Composite : IIdentifiable
{
    public byte[] Version { get; private set; } = Array.Empty<byte>(); // EF Core optimistic concurrency
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

     public virtual Guid Id { get; } = Guid.NewGuid();

    protected Composite() { }

    protected void UpdateTimestamp() => UpdatedAt = DateTimeOffset.UtcNow;

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
