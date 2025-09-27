using System;

namespace Application.Interfaces.IntergrationEvent;

public interface IIntegrationEventPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)  where T : class;
}
