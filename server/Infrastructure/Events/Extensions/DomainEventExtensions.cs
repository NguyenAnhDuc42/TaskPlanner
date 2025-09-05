using System;
using Domain.Common.Interfaces;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.Events.Extensions;

public static class DomainEventExtensions
{
    public static IReadOnlyCollection<IDomainEvent> CollectDomainEvents(this ChangeTracker changeTracker)
    {
        return changeTracker.Entries<Aggregate>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList().AsReadOnly();
    }

    public static void ClearDomainEvents(this ChangeTracker changeTracker)
    {
        var aggregates = changeTracker.Entries<Aggregate>()
            .Select(e => e.Entity);

        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();
    }
}

public sealed record AggregateDomainEvents(Aggregate Aggregate, IReadOnlyCollection<IDomainEvent> Events);
