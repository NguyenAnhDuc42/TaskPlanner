using System;
using Application.Common.Interfaces;


namespace Application.EventHandlers.Interface;

public interface IIntegrationEventHandler<TEvent> where TEvent : IIntegrationEvent
{
    Task<IntegrationEventHandlingResult> HandleAsync(TEvent @event,CancellationToken cancellationToken = default);
}
