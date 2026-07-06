namespace Api;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration config)
    {
        config.ValidateRequiredSecrets();

        services.AddHandlerPipeline();
        services.AddWorkspaceServices();
        services.AddDatabaseAndCaching(config);
        services.AddCoreServices(config);
        services.AddEmail(config);
        services.AddSignalR();
        services.AddAppAuthentication(config);

        return services;
    }
}
