namespace Api;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration config)
    {
        services.AddHandlerPipeline();
        services.AddWorkspaceServices();
        services.AddDatabaseAndCaching(config);
        services.AddCoreServices(config);
        services.AddEmail(config);
        services.AddObjectStorage(config);

        // Redis backplane temporarily reverted — was crashing every hub connection in production
        // (Redis unreachable on Railway, taking down OnConnectedAsync with it). Re-add once the
        // Railway Redis connectivity issue is root-caused; single-instance prod loses nothing by
        // running in-memory in the meantime.
        services.AddSignalR();

        services.AddAppAuthentication(config);

        return services;
    }
}
