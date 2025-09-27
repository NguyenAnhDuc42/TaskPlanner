
using Application.Common.Interfaces;
using Application.EventHandlers;

namespace Application.Interfaces.IntergrationEvent;

public interface IIntegrationEventDispatcher
{
    Task<IntegrationEventHandlingResult> DispatchAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default);
}
