

using Application.EventHandlers;
using Application.EventHandlers.Interface;
using Application.Interfaces;
using Confluent.Kafka;
using Infrastructure.Events.IntergrationsEvents;
using Infrastructure.Interfaces;
using Infrastructure.OutBox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Dependencies.Registrations;

public static class OutBoxRegistrations
{
    public static IServiceCollection AddIntegrationEvents(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(IntegrationEventOptions.SectionName);

        services.AddOptions<IntegrationEventOptions>()
                .Bind(section)
                .Validate(o => o.Topics != null && o.Topics.Count > 0, "IntegrationEvents:Topics must contain at least one mapping")
                .Validate(o => o.Kafka != null && !string.IsNullOrWhiteSpace(o.Kafka.BootstrapServers), "IntegrationEvents:Kafka:BootstrapServers is required")
                .Validate(o => o.Retry != null && o.Retry.MaxRetries >= 0 && o.Retry.InitialDelaySeconds >= 0 && o.Retry.BackoffMultiplier >= 1.0, "IntegrationEvents:Retry values invalid")
                .Validate(o => o.Outbox != null && o.Outbox.BatchSize >= 1 && o.Outbox.PollDelaySeconds >= 1, "IntegrationEvents:Outbox values invalid");


        services.AddSingleton<IProducer<string, string>>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<IntegrationEventOptions>>();
            var config = new ProducerConfig { BootstrapServers = opts.Value.Kafka.BootstrapServers };
            return new ProducerBuilder<string, string>(config).Build();
        });
        services.AddSingleton<Func<string, IConsumer<string, string>>>(sp =>
         groupId =>
        {
            var opts = sp.GetRequiredService<IOptions<IntegrationEventOptions>>().Value;
            var config = new ConsumerConfig
            {
                BootstrapServers = opts.Kafka.BootstrapServers,
                GroupId = groupId,
                EnableAutoCommit = false,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
            return new ConsumerBuilder<string, string>(config).Build();
        });
        services.AddScoped<IOutboxService, OutboxService>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IIntegrationEventDispatcher, IntegrationEventDispatcher>();
        services.AddSingleton<IIntegrationEventPublisher, IntegrationEventPublisher>();
        services.AddSingleton<IDeadLetterSink, TopicDeadLetterSink>();
        // services.AddSingleton<IEventTypeMapper, EventTypeMapper>()

        return services;
    }

}
