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

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTime.UtcNow;

  
    protected Entity() { }
    protected Entity(Guid id) => Id = id;

    protected void UpdateTimestamp() => UpdatedAt = DateTimeOffset.UtcNow;

    public override bool Equals(object? obj) => obj is Entity other && Id.Equals(other.Id);
    public override int GetHashCode() => Id.GetHashCode();
}
