namespace Api;

public static class CoreServicesExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<CurrentUserService>();
        services.AddScoped<CookieService>();
        services.AddScoped<TokenService>();
        services.AddScoped<RealtimeService>();
        services.AddScoped<SyncQueryService>();
        services.AddHttpContextAccessor();

        services.Configure<CursorEncryptionOptions>(config.GetSection(CursorEncryptionOptions.SectionName));
        services.AddScoped<CursorHelper>();
        services.AddScoped<PermissionService>();
        services.AddScoped<NotificationService>();
        services.AddHostedService<CleanupService>();
        services.AddScoped<IdempotencyService>();

        return services;
    }
}
