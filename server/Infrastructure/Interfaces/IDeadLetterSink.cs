using System;

namespace Infrastructure.Interfaces;

public interface IDeadLetterSink
{
    Task SaveAsync(string payload, string? eventType, string reason, CancellationToken cancellationToken = default);
}
