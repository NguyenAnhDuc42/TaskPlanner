using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Events.Space;
using Application.Common.Interfaces;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Domain.Enums.RelationShip;
using Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;

namespace Application.EventHandlers.DomainEventHandlers.SpaceHandlers;

public class CreateSpaceEventHandler() : IDomainEventHandler<SpaceCreatedEvent>
{
    public async Task Handle(SpaceCreatedEvent notification, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
}
