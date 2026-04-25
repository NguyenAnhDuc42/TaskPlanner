using Application;
using Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Dependencies.Registrations
{
    public static class DomainEventsRegistration
    {
        public static IServiceCollection AddDomainEvents(this IServiceCollection services)
        {
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
}
