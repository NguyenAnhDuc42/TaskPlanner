using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Events.Folder;
using Application.Common.Interfaces;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Domain.Enums.RelationShip;
using Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;

namespace Application.EventHandlers.DomainEventHandlers.FolderHandlers;

public class CreateFolderEventHandler(
    ILogger<CreateFolderEventHandler> logger, 
    IDataBase db, 
    IRealtimeService realtime
) : IDomainEventHandler<FolderCreatedEvent>
{
    public async Task Handle(FolderCreatedEvent notification, CancellationToken cancellationToken)
    {
        // View seeding is now handled inline in CreateFolderHandler and CreateWorkspaceHandler.
        // This handler is preserved for future asynchronous side effects.
        await Task.CompletedTask;
    }
}
