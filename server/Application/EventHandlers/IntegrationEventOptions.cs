using System;

namespace Application.EventHandlers;

public class IntegrationEventOptions
{
    public const string SectionName = "IntegrationEvents";

    public Dictionary<string, string> Topics { get; set; } = new();
    public KafkaOptions Kafka { get; set; } = new();
    public RetryOptions Retry { get; set; } = new();
}

public class KafkaOptions
{
    public string BootstrapServers { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public int MaxConcurrency { get; set; } = 10;
    public int ProcessingTimeoutSeconds { get; set; } = 300;
}

public class RetryOptions
{
    public int MaxRetries { get; set; } = 3;
    public int InitialDelaySeconds { get; set; } = 1;
    public double BackoffMultiplier { get; set; } = 2.0;
}

public class OutboxOptions
{
    public const string SectionName = "Outbox";

    public long AdvisoryLockKey { get; set; } = 1234567890;
    public int PollDelaySeconds { get; set; } = 5;
    public int BatchSize { get; set; } = 50;
}
