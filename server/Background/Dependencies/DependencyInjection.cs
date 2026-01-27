using System;
using Application.Interfaces;
using Background.Jobs;
using Background.Services;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Background.Dependencies;

public static class DependencyInjection
{
    public static IServiceCollection AddBackground(this IServiceCollection services, IConfiguration config)
    {
        // Hangfire with in-memory storage (use PostgreSQL in production)
        services.AddHangfire(c => c
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseMemoryStorage());

        services.AddHangfireServer();

        // Background job service
        services.AddSingleton<IBackgroundJobService, HangfireBackgroundJobService>();

        // Jobs (registered for DI so Hangfire can resolve them)
        services.AddScoped<MemberCleanupJob>();
        services.AddScoped<ProcessOutboxJob>();

        return services;
    }
}
