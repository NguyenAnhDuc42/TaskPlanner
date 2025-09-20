using System;
using Application.Interfaces.Outbox;
using Domain.OutBox;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.OutBox;

public class OutboxService : IOutboxService
{
    private readonly TaskPlanDbContext _dbContext;
    public OutboxService(TaskPlanDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task EnqueueAsync(string type, string payloadJson, string? routingKey = null, string? deduplicationKey = null, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(deduplicationKey))
        {
            var exists = await _dbContext.OutboxMessages.AnyAsync(x => x.DeduplicationKey == deduplicationKey && !x.IsProcessed, ct);
            if (exists) return; // Skip enqueueing duplicate message
        }
        var msg = new OutboxMessage(type, payloadJson, routingKey, deduplicationKey);
        _dbContext.Add(msg);

        // Do NOT call SaveChanges here — caller will commit UoW.
    }
}
