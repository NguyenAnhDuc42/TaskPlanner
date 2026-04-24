using Application.Interfaces.Data;
using Domain.Common;
using Domain.Entities;
using Domain.Events.Workspace;
using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Application.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;

namespace Application.EventHandlers.DomainEventHandlers.WorkspaceHandlers;

public class CreateWorkspaceEventHandler(
    ILogger<CreateWorkspaceEventHandler> logger, 
    IDataBase db, 
    IRealtimeService realtime
) : IDomainEventHandler<CreatedWorkspaceEvent>
{
    public async Task Handle(CreatedWorkspaceEvent notification, CancellationToken cancellationToken)
    {
        // Seeding is now handled inline in CreateWorkspaceHandler for performance and atomicity.
        // This handler is preserved for future asynchronous side effects (e.g., integrations, email notifications).
        await Task.CompletedTask;
    }
}
