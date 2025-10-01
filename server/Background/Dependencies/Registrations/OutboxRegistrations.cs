using System;
using Background.Workers;
using Microsoft.Extensions.DependencyInjection;

namespace Background.Dependencies.Registrations;

public static class OutboxRegistrations
{
    public static IServiceCollection AddOutboxWorker(this IServiceCollection services)
    {
        services.AddHostedService<OutboxHostedWorker>();
        services.AddHostedService<KafkaConsumerWorker>();
        services.AddHostedService<RetryRelayWorker>();
        return services;
    }
}
