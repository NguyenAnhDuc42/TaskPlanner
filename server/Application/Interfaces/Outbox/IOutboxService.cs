using System;

namespace Application.Interfaces.Outbox;

public interface IOutboxService
{
    /// <summary>
    /// Enqueue a message to be published later. Should be called inside the same UoW/transaction as domain changes.
    /// </summary>
    Task EnqueueAsync(string type, string payloadJson, string? routingKey = null, string? deduplicationKey = null, CancellationToken ct = default);
}
