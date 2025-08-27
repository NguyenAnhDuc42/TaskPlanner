using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Domain.Common.Interfaces;

namespace Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    [Timestamp] // EF Core optimistic concurrency
    public byte[] Version { get; private set; } = Array.Empty<byte>();

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // === Domain Events ===
    private readonly ConcurrentQueue<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.ToArray();
    public bool HasDomainEvents => !_domainEvents.IsEmpty;

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

    protected Entity() { }
    protected Entity(Guid id) => Id = id;

    protected void UpdateTimestamp() => UpdatedAt = DateTime.UtcNow;

    public override bool Equals(object? obj) => obj is Entity other && Id.Equals(other.Id);
    public override int GetHashCode() => Id.GetHashCode();
}
