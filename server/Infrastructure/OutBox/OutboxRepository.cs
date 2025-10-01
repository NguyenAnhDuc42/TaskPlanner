using System;
using Application.Interfaces;
using Domain.OutBox;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.OutBox;

public class OutboxRepository : IOutboxRepository
{
    private readonly OutboxDbContext _dbContext;
    public OutboxRepository(OutboxDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }
    public async Task<bool> ExistsAsync(string deduplicationKey, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OutboxMessages.AsNoTracking().AnyAsync(m => m.DeduplicationKey == deduplicationKey, cancellationToken);
    }

    public Task<IReadOnlyList<OutboxMessage>> GetPendingWithLockAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task IncrementAttemptsAndRescheduleAsync(Guid id, DateTimeOffset nextAvailableAt, TimeSpan backoff, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task MarkDeadLetterAsync(Guid id, string reason, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task MarkSentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await _dbContext.OutboxMessages.AddAsync(message,cancellationToken);
    }

    public Task SaveBatchAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
