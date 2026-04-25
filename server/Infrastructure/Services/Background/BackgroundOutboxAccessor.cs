using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Background.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Background;

public class BackgroundOutboxAccessor : IBackgroundOutboxAccessor
{
    private readonly TaskPlanDbContext _dbContext;

    public BackgroundOutboxAccessor(TaskPlanDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<OutboxMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        return await _dbContext.OutboxMessages
            .Where(m => m.State == OutboxState.Pending && (m.ScheduledAtUtc == null || m.ScheduledAtUtc <= now))
            .OrderBy(m => m.OccurredOnUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public void ResetTracking(OutboxMessage message)
    {
        _dbContext.ChangeTracker.Clear();
        _dbContext.OutboxMessages.Attach(message);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
