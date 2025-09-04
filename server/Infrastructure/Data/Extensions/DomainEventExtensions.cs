using System;
using Domain.Common.Interfaces;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.Data.Extensions;

public static class DomainEventExtensions
{
    public static IReadOnlyCollection<AggregateDomainEvents> CollectAggregateDomainEvents(this ChangeTracker changeTracker)
    {
        var aggregates = changeTracker.Entries<Aggregate>()
        .Select(e => e.Entity).ToList();


        var result = new List<AggregateDomainEvents>(aggregates.Count());
        foreach (var aggregate in aggregates)
        {
            var events = aggregate.DomainEvents is not null && aggregate.DomainEvents.Any()
            ? aggregate.DomainEvents.ToList().AsReadOnly()
            : Array.Empty<IDomainEvent>().AsReadOnly();

            if (events.Count() > 0) result.Add(new AggregateDomainEvents(aggregate, events));
        }
        return result.AsReadOnly();
    }
    public static IReadOnlyCollection<IDomainEvent> FlattenEvents(this IReadOnlyCollection<AggregateDomainEvents> snapshots)
    {
        return snapshots.SelectMany(s => s.Events).ToList().AsReadOnly();
    }

    public static void ClearDomainEventsFromSnapshot(this IReadOnlyCollection<AggregateDomainEvents> snapshots)
    {
        if (snapshots == null || snapshots.Count == 0) return;

        foreach (var snap in snapshots)
        {
            try
            {
                if (snap.Aggregate.DomainEvents is { Count: > 0 })
                {
                    snap.Aggregate.ClearDomainEvents();
                }
            }
            catch
            {
                throw;
            }
        }
    }
}

public sealed record AggregateDomainEvents(Aggregate Aggregate, IReadOnlyCollection<IDomainEvent> Events);
