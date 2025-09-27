using System;
using System.Text.Json;
using Application.Common.Interfaces;
using Application.Interfaces;
using Application.Interfaces.IntergrationEvent;
using Application.Interfaces.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Background.Workers;

public class OutboxHostedWorker : BackgroundService
{
    private readonly IServiceProvider _provider;
    private readonly ILogger<OutboxHostedWorker> _logger;
    private readonly TimeSpan _pollDelay = TimeSpan.FromSeconds(5);
    private readonly long _advisoryLockKey; // numeric key used for pg_try_advisory_lock


    public OutboxHostedWorker(IServiceProvider provider, ILogger<OutboxHostedWorker> logger, IConfiguration config)
    {
        _provider = provider;
        _logger = logger;
        _advisoryLockKey = config.GetValue<long>("Outbox:AdvisoryLockKey", 1234567890);
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IOutboxProcessorDbContext>();
                var connection = dbContext.Database.GetDbConnection();
                await connection.OpenAsync(stoppingToken);

                bool gotLock;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT pg_try_advisory_lock(@param)";
                    var param = command.CreateParameter();
                    param.ParameterName = "param";
                    param.Value = _advisoryLockKey;
                    command.Parameters.Add(param);
                    var result = await command.ExecuteScalarAsync(stoppingToken);
                    gotLock = result is bool b && b;
                }

                if (!gotLock)
                {
                    await Task.Delay(_pollDelay, stoppingToken);
                    continue; // Another instance is processing the outbox
                }
                try
                {
                    using var tx = await dbContext.Database.BeginTransactionAsync(stoppingToken);
                    var message = await dbContext.OutboxMessages
                        .Where(m => !m.IsProcessed && m.AvailableAt <= DateTimeOffset.Now)
                        .OrderBy(m => m.OccurredOn)
                        .Take(50)
                        .ToListAsync(stoppingToken);
                    foreach (var msg in message)
                    {
                        try
                        {
                            var eventType = Type.GetType(msg.Type);
                            if (eventType == null)
                            {
                                _logger.LogError("Unknown event type: {EventType} for message {MessageId}", msg.Type, msg.Id);
                                msg.SetError($"Unknown event type: {msg.Type}");
                                msg.IncrementAttempts(TimeSpan.FromHours(1));
                                continue;
                            }

                            var @event = JsonSerializer.Deserialize(msg.Payload, eventType) as IIntegrationEvent;
                            if (@event == null)
                            {
                                _logger.LogError("Failed to deserialize message {MessageId}", msg.Id);
                                msg.SetError("Deserialization failed");
                                msg.IncrementAttempts(TimeSpan.FromHours(1));
                                continue;
                            }

                            var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
                            await publisher.PublishAsync(@event, stoppingToken);
                            msg.MarkProcessed();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to process outbox message {MessageId}", msg.Id);
                            var backoff = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, msg.Attempts), 300));
                            msg.IncrementAttempts(backoff);
                            msg.SetError(ex.Message);
                        }
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);
                    await tx.CommitAsync(stoppingToken);
                }
                finally
                {
                    using var releaseCommand = connection.CreateCommand();
                    releaseCommand.CommandText = "SELECT pg_advisory_unlock(@param)";
                    var param = releaseCommand.CreateParameter();
                    param.ParameterName = "param";
                    param.Value = _advisoryLockKey;
                    releaseCommand.Parameters.Add(param);
                    await releaseCommand.ExecuteScalarAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OutboxHostedWorker");

            }
            await Task.Delay(_pollDelay, stoppingToken);
        }

    }
}
