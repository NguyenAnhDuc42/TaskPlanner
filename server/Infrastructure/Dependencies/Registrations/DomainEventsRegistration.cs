
using Application;
using Application.Interfaces;
using Application.Common.Interfaces;
using Domain.Common.Interfaces;
using Infrastructure.Events.DomainEvents;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Dependencies.Registrations;

public static class DomainEventsRegistration
{
    public static IServiceCollection AddDomainEvents(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        var handlers = typeof(ApplicationAssemblyMarker).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } &&
                        t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)));

        foreach (var handler in handlers)
        {
            var interfaces = handler.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>));
            
            foreach (var iface in interfaces)
            {
                services.AddScoped(iface, handler);
            }
        }

        return services;
    }
}
