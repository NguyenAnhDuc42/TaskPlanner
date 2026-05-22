using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Background;

public static class DependencyInjection
{
    public static IServiceCollection AddBackground(this IServiceCollection services, IConfiguration config)
    {
        #region Hangfire Configuration
        services.AddHangfire(cfg =>
        {
            cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
               .UseSimpleAssemblyNameTypeSerializer()
               .UseRecommendedSerializerSettings()
               .UsePostgreSqlStorage(
                   c => c.UseNpgsqlConnection(config.GetConnectionString("TaskPlannerHangfire")),
                   new PostgreSqlStorageOptions
                   {
                       QueuePollInterval = TimeSpan.FromSeconds(30)
                   });
        });

        services.AddHangfireServer(options => 
        {
            options.WorkerCount = 2;
            options.SchedulePollingInterval = TimeSpan.FromMinutes(5);
            options.HeartbeatInterval = TimeSpan.FromMinutes(5);
            options.ServerCheckInterval = TimeSpan.FromMinutes(5);
        });
        #endregion

        #region Background Jobs & Services
        services.AddScoped<DatabaseKeepAliveService>();
        services.AddSingleton<IBackgroundJobClient, BackgroundJobClient>();
        services.AddSingleton<IRecurringJobManager, RecurringJobManager>();
        #endregion

        return services;
    }
}

