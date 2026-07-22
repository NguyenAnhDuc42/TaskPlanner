using StackExchange.Redis;

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

        // AbortOnConnectFail defaults to true, which means: if the very first connection attempt
        // fails (e.g. Redis/private networking isn't fully up yet the instant this container
        // starts — a real race we hit on Railway), the multiplexer gives up permanently instead of
        // retrying. false lets it keep retrying in the background until Redis becomes reachable,
        // instead of taking every hub connection down with it forever.
        services.AddSignalR()
            .AddStackExchangeRedis(config.GetConnectionString("Redis")!, options =>
            {
                options.Configuration.AbortOnConnectFail = false;
                options.Configuration.ChannelPrefix = RedisChannel.Literal("taskplanner-signalr");
            });

        services.AddAppAuthentication(config);

        return services;
    }
}
