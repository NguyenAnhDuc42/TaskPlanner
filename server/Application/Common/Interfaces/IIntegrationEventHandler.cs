using System;
using Application.EventHandlers;

namespace Application.Common.Interfaces;

public interface IIntegrationEventHandler<TEvent> where TEvent : IIntegrationEvent
{
    Task<IntegrationEventHandlingResult> HandleAsync(TEvent @event,CancellationToken cancellationToken = default);
}
