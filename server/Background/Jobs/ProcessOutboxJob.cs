using Application.Interfaces;
using Domain.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Infrastructure.Data;
using Domain.Common;
using Domain.Events;

namespace Background.Jobs;

public class ProcessOutboxJob
{
    private readonly TaskPlanDbContext _dbContext;
    private readonly IMediator _mediator;
    private readonly ILogger<ProcessOutboxJob> _logger;

    public ProcessOutboxJob(
        TaskPlanDbContext dbContext,
        IMediator mediator,
        ILogger<ProcessOutboxJob> logger)
    {
        _dbContext = dbContext;
        _mediator = mediator;
        _logger = logger;
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Type> TypeCache = new();

    public async Task RunAsync()
    {
        var messages = await _dbContext.OutboxMessages
            .Where(m => m.State == OutboxState.Pending)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(50) // Increased batch size for better throughput
            .ToListAsync();

        if (!messages.Any()) return;

        foreach (var message in messages)
        {
            try
            {
                var eventType = TypeCache.GetOrAdd(message.Type, name => typeof(BaseDomainEvent).Assembly.GetTypes().FirstOrDefault(t => t.Name == name)!);

                if (eventType == null)
                {
                    message.MarkAsFailed($"Could not find type: {message.Type}");
                    continue;
                }

                var domainEvent = JsonSerializer.Deserialize(message.Content, eventType) as IDomainEvent;
                if (domainEvent == null)
                {
                    message.MarkAsFailed($"Could not deserialize content for: {message.Type}");
                    continue;
                }

                await _mediator.Publish(domainEvent);
                message.MarkAsProcessed();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox message {Id}", message.Id);
                message.MarkAsFailed(ex.Message);
            }
        }

        await _dbContext.SaveChangesAsync();
    }
}
