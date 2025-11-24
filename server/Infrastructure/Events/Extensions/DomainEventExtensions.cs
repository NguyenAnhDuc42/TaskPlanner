using System;
using Domain.Common.Interfaces;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.Events.Extensions;

public static class DomainEventExtensions
{
    public static IReadOnlyCollection<IDomainEvent> CollectDomainEvents(this ChangeTracker changeTracker)
    {
        return changeTracker.Entries<Entity>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList().AsReadOnly();
    }

    public static void ClearDomainEvents(this ChangeTracker changeTracker)
    {
        var entities = changeTracker.Entries<Entity>()
            .Select(e => e.Entity);

        foreach (var entity in entities)
            entity.ClearDomainEvents();
    }
}

public sealed record EntityDomainEvents(Entity Entity, IReadOnlyCollection<IDomainEvent> Events);
