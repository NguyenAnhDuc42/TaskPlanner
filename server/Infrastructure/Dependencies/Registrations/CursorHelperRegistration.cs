using Infrastructure.Helper;
using Infrastructure.Helper.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Dependencies.Registrations;

public static class CursorHelperRegistration
{
    public static IServiceCollection AddCursorHelper(
        this IServiceCollection services, IConfiguration config)
    {
        services.Configure<CursorEncryptionOptions>(
            config.GetSection(CursorEncryptionOptions.SectionName));

        services.AddSingleton<CursorHelper>();

        return services;
    }
}
