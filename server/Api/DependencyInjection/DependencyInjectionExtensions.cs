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

        // Redis backplane reverted AGAIN — AbortOnConnectFail=false didn't fix it, meaning Redis is
        // genuinely unreachable from this container (not just a startup race). Do not re-add until
        // reachability is confirmed independently of a live hub connection (e.g. a throwaway
        // endpoint or Railway shell that actually pings redis.railway.internal:6379), not by
        // shipping it straight to production again. Single-instance prod loses nothing by staying
        // in-memory in the meantime.
        services.AddSignalR();

        services.AddAppAuthentication(config);

        return services;
    }
}
