using System;
using System.Data;
using Application.Common.Exceptions;
using Application.Interfaces;
using Dapper;
using Domain.Enums;
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

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingWithLockAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var messages = await _dbContext.OutboxMessages.AsNoTracking()
        .Where(m => m.State == OutboxState.Pending && m.AvailableAtUtc <= DateTimeOffset.UtcNow)
        .OrderBy(m => m.OccurredOnUtc)
        .Take(batchSize).ToListAsync(cancellationToken);
        return messages;
    }

    public async Task IncrementAttemptsAndRescheduleAsync(Guid id, DateTimeOffset nextAvailableAt, TimeSpan backoff, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.OutboxMessages.FindAsync(id, cancellationToken);
        if (message == null) return;
        if (message.State != OutboxState.Pending) return; // no-op if not pending

        message.IncrementAttemptsAndReschedule(backoff, nextAvailableAt);
        

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
        await _dbContext.OutboxMessages.AddAsync(message, cancellationToken);
    }

    public async Task SaveBatchAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default)
    {
        var list = (messages ?? Enumerable.Empty<OutboxMessage>()).ToList();
        if (!list.Any()) return;

        var keys = list
            .Select(m => m.DeduplicationKey)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct()
            .ToArray();

        if (keys.Length > 0)
        {
            var duplicateKeys = await _dbContext.OutboxMessages
                .Where(m => m.State == OutboxState.Pending && keys.Contains(m.DeduplicationKey))
                .Select(m => m.DeduplicationKey)
                .ToListAsync(cancellationToken);

            if (duplicateKeys.Any())
                throw new DuplicateEventException(duplicateKeys.First());
        }
        _dbContext.OutboxMessages.AddRange(list);
    }
}

