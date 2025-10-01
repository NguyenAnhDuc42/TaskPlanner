using System;
using Application.Common.Interfaces;

namespace Application.EventHandlers.Interface;

public interface IIntegrationEventDispatcher
{
    Task<IntegrationEventHandlingResult> DispatchAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default);
}

