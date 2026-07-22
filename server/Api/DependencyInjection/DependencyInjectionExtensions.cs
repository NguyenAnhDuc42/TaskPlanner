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

        // Root cause found via an isolated diagnostic endpoint: StackExchange.Redis 2.13.1's
        // ConfigurationOptions.Parse() mishandles our "redis://user:pass@host:port" string,
        // resolving to a duplicated-port endpoint that can never connect. Using the
        // configure-delegate-only overload here and building ConfigurationOptions ourselves via
        // RedisConnectionHelper (System.Uri parsing) bypasses that buggy string parser entirely -
        // confirmed working (Connected: True, clean single-port endpoint) before wiring this back in.
        var redisConnStr = config.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("ConnectionStrings:Redis is required for the SignalR backplane.");

        services.AddSignalR()
            .AddStackExchangeRedis(options =>
            {
                options.Configuration = RedisConnectionHelper.ParseRedisUrl(redisConnStr);
                options.Configuration.AbortOnConnectFail = false;
                options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("taskplanner-signalr");
            });

        services.AddAppAuthentication(config);

        return services;
    }
}
