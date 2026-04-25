using Application.Interfaces;
using Background.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Background.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Background.Dependencies;

public static class DependencyInjection
{
    public static IServiceCollection AddBackground(this IServiceCollection services, IConfiguration config)
    {
        services.AddHangfire(cfg =>
        {
            cfg
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(
                    c => c.UseNpgsqlConnection(config.GetConnectionString("TaskPlannerHangfire")),
                    new PostgreSqlStorageOptions
                    {
                        QueuePollInterval = TimeSpan.FromMinutes(40)
                    });
        });
        services.AddHangfireServer(options => {
            options.WorkerCount = 2;
            options.SchedulePollingInterval = TimeSpan.FromMinutes(40);
            options.ServerCheckInterval = TimeSpan.FromMinutes(40);
        });

        services.AddSingleton<IBackgroundJobClient, BackgroundJobClient>();
        services.AddSingleton<IRecurringJobManager, RecurringJobManager>();

        services.AddSingleton<IBackgroundJobService, HangfireBackgroundJobService>();
        services.AddSingleton<DomainEventTypeProvider>();
        services.AddScoped<Background.Jobs.ProcessOutboxJob>();
        services.AddSingleton<HangfireJobScheduler>();

        return services;
    }
}
