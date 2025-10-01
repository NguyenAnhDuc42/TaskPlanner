using System;

namespace Application.Common.Interfaces;

public interface IIntegrationEvent
{
    Guid Id { get; }
    string? CorrelationId { get; }
    DateTimeOffset OccurredOnUtc { get; }
}
