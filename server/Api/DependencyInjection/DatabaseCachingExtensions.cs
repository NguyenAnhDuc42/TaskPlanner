namespace Api;

public static class DatabaseCachingExtensions
{
    public static IServiceCollection AddDatabaseAndCaching(this IServiceCollection services, IConfiguration config)
    {
        var perfSettings = config.GetSection(PerformanceSettings.SectionName).Get<PerformanceSettings>()
                           ?? new PerformanceSettings();

        var cacheSettings = config.GetSection(CacheSettings.SectionName).Get<CacheSettings>()
                           ?? new CacheSettings();
        services.Configure<CacheSettings>(config.GetSection(CacheSettings.SectionName));

        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 1024 * 1024 * cacheSettings.MemoryCacheLimitMB;
        });

        #pragma warning disable EXTEXP0018
        services.AddHybridCache(options =>
        {
            options.MaximumPayloadBytes = 1024 * 1024; // 1MB
            options.MaximumKeyLength = 512;
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(5)
            };
        });
        #pragma warning restore EXTEXP0018

        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        if (!services.Any(d => d.ServiceType == typeof(DbContextOptions<TaskPlanDbContext>) || d.ServiceType == typeof(TaskPlanDbContext)))
        {
            services.AddDbContextPool<TaskPlanDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: perfSettings.DatabaseMaxRetryCount,
                        maxRetryDelay: TimeSpan.FromSeconds(perfSettings.DatabaseMaxRetryDelaySeconds),
                        errorCodesToAdd: null);
                });
            }, poolSize: perfSettings.DbContextPoolSize);
        }

        return services;
    }
}
