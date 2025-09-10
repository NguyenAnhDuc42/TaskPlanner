using Infrastructure.Dependencies.Registrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IConfiguration config)
    {
        services.AddSignalR();
        services
            .AddDbContextInfrastructure(connectionString)
            .AddRepositoriesAndServices()
            .AddJwtAuthentication(config)
            .AddDomainEvents()
            .AddCursorHelper(config)
            .AddRealtimeServices();

        return services;
    }
}
