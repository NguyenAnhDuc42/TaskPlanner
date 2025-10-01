using System.Data;
using Application.Common.Exceptions;
using Application.EventHandlers.Interface;
using Application.Interfaces;
using Dapper;
using Domain.OutBox;
using Infrastructure.Data;


namespace Infrastructure.OutBox;

public class OutboxService : IOutboxService
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly IDbConnection _dbConnection;

    public OutboxService(IOutboxRepository outboxRepository, IDbConnection dbConnection)
    {
        _outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
        _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
    }

    public async Task EnqueueAsync(string eventType, string payloadJson, string? routingKey = null, string? deduplicationKey = null, string? createdBy = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(eventType)) throw new ArgumentException(nameof(eventType));
        if (string.IsNullOrWhiteSpace(payloadJson)) throw new ArgumentException(nameof(payloadJson));

        if (!string.IsNullOrWhiteSpace(deduplicationKey))
        {
            var duplicate = await _outboxRepository.ExistsAsync(deduplicationKey, ct);
            if (duplicate) throw new DuplicateEventException(deduplicationKey);
        }

        var message = new OutboxMessage(eventType, payloadJson, routingKey, deduplicationKey, createdBy);
        await _outboxRepository.SaveAsync(message, ct);
    }

    public async Task EnqueueBatchAsync(IEnumerable<(string eventType, string payloadJson, string? routingKey, string? deduplicationKey, string? createdBy)> events, CancellationToken cancellationToken = default)
    {
        var messagesToSave = events.Select(e => new OutboxMessage(e.eventType, e.payloadJson, e.routingKey, e.deduplicationKey, e.createdBy)).ToList();
        var deduplicationKeys = messagesToSave
        .Where(e => !string.IsNullOrWhiteSpace(e.DeduplicationKey))
        .Select(e => e.DeduplicationKey)
        .Distinct()
        .ToList();

        if (!deduplicationKeys.Any())
        {
            // 3. Save the batch immediately if no deduplication keys exist.
            await _outboxRepository.SaveBatchAsync(messagesToSave, cancellationToken);
            return;
        }

        const string sql =
        @"SELECT deduplication_key 
          FROM outbox_messages
          WHERE deduplication_key = ANY(@Keys)
          AND state = 0;";

        var parameters = new { Keys = deduplicationKeys.ToArray() };
        var duplicateKeys = await _dbConnection.QueryAsync<string>(sql, parameters);

        var duplicateKeysList = duplicateKeys.ToList();
        if (duplicateKeysList.Any())
        {
            var duplicatedKeyString = string.Join(", ", duplicateKeysList);
            throw new DuplicateEventException(duplicatedKeyString);
        }

        await _outboxRepository.SaveBatchAsync(messagesToSave, cancellationToken);
    }
}