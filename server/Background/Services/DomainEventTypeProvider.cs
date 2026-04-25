using Domain.Common;
using Domain.Common.Interfaces;
using Domain.Events;

namespace Background.Services;

public class DomainEventTypeProvider
{
    private readonly IReadOnlyDictionary<string, Type> _typeCache;

    public DomainEventTypeProvider()
    {
        _typeCache = typeof(BaseDomainEvent).Assembly.GetTypes()
            .Where(t => typeof(IDomainEvent).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToDictionary(t => t.Name, t => t);
    }

    public Type? GetEventType(string typeName)
    {
        return _typeCache.TryGetValue(typeName, out var type) ? type : null;
    }
}
