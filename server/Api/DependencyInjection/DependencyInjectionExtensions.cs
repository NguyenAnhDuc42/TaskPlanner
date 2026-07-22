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

        services.AddSignalR()
            .AddStackExchangeRedis(config.GetConnectionString("Redis")!, options =>
            {
                options.Configuration.ChannelPrefix = RedisChannel.Literal("taskplanner-signalr");
            });

        services.AddAppAuthentication(config);

        return services;
    }
}
