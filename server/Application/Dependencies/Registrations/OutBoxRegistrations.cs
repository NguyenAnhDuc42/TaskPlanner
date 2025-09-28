using System;
using Application.EventHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Dependencies.Registrations;

public static class OutBoxRegistrations
{
    public static IServiceCollection AddOutbox(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<KafkaOptions>(config.GetSection("IntegrationEvents:Kafka"));
        services.Configure<RetryOptions>(config.GetSection("IntegrationEvents:Retry")); 
        services.Configure<OutboxOptions>(config.GetSection("Outbox"));
        return services;
    }
}