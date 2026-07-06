namespace Api;

public static class ObjectStorageExtensions
{
    public static IServiceCollection AddObjectStorage(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<ObjectStorageSettings>(config.GetSection(ObjectStorageSettings.SectionName));
        services.AddScoped<ObjectStorageService>();

        return services;
    }
}
