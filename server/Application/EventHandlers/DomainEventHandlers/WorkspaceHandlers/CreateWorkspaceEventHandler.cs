using Application.Interfaces.Data;
using Domain.Common;
using Domain.Entities;
using Domain.Events.Workspace;
using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Application.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;

namespace Application.EventHandlers.DomainEventHandlers.WorkspaceHandlers;

public class CreateWorkspaceEventHandler() : IDomainEventHandler<CreatedWorkspaceEvent>
{
    public async Task Handle(CreatedWorkspaceEvent notification, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
}
