using Microsoft.Extensions.DependencyInjection;
using Scrutor;

namespace Infrastructure.Dependencies.Registrations;

public static class ScrutorRegistration
{
    public static IServiceCollection AddRepositoriesAndServices(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<InfrastructureAssemblyMarker>()
            .AddClasses(c => c.Where(t => t.Name.EndsWith("Repository")))
                .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                .AsMatchingInterface()
                .WithScopedLifetime()
            .AddClasses(c => c.Where(t => t.Name.EndsWith("Service")))
                .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                .AsMatchingInterface()
                .WithScopedLifetime()
            .AddClasses(c => c.InNamespaces("Infrastructure")
                             .Where(t => !t.Name.EndsWith("Repository") 
                                      && !t.Name.EndsWith("Service")))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
        );

        return services;
    }
}

