using System;
using Application.Helper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Dependencies.Registrations;

public static class CursorPaginationRegistration
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