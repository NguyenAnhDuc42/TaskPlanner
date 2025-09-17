using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common.Interfaces;

namespace Domain.Common;

public abstract class Composite
{
    [Timestamp] public byte[] Version { get; private set; } = Array.Empty<byte>(); // EF Core optimistic concurrency
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    [NotMapped] private readonly List<IDomainEvent> _domainEvents = new();
    [NotMapped] public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

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
