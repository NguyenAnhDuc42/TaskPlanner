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

public class CreateSpaceEventHandler(
    ILogger<CreateSpaceEventHandler> logger, 
    IDataBase db, 
    IRealtimeService realtime
) : IDomainEventHandler<SpaceCreatedEvent>
{
    public async Task Handle(SpaceCreatedEvent notification, CancellationToken cancellationToken)
    {
        // View seeding is now handled inline in CreateSpaceHandler and CreateWorkspaceHandler.
        // This handler is preserved for future asynchronous side effects.
        await Task.CompletedTask;
    }
}
