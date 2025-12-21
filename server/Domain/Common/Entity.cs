using Domain.Common.Interfaces;

namespace Domain.Common;

public abstract class Entity 
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; protected set; }
    public Guid? CreatorId { get; protected set; }
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected Entity() { }
    protected Entity(Guid id) => Id = id;

    protected void UpdateTimestamp() => UpdatedAt = DateTimeOffset.UtcNow;
    public override bool Equals(object? obj) => obj is Entity other && Id.Equals(other.Id);
    public override int GetHashCode() => Id.GetHashCode();

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

    public void SoftDelete() => DeletedAt = DateTimeOffset.UtcNow;
}
