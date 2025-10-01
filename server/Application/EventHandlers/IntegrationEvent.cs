using System;
using Application.Common.Interfaces;

namespace Application.EventHandlers;

public abstract class IntegrationEvent : IIntegrationEvent
{
    public Guid Id { get; init; }
    public string? CorrelationId { get; init; }
    public DateTimeOffset OccurredOnUtc { get; init; }
}   
