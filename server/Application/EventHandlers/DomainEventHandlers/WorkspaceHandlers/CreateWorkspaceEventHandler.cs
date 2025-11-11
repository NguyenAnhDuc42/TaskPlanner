using System;
using Application.Interfaces.Repositories;
using Domain.Entities.Relationship;
using Domain.Events.Workspace;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers.DomainEventHandlers.WorkspaceHandlers;

public class CreateWorkspaceEventHandler : INotificationHandler<CreatedWorkspaceEvent>
{
    private readonly ILogger<CreateWorkspaceEventHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;
    public CreateWorkspaceEventHandler(ILogger<CreateWorkspaceEventHandler> logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(CreatedWorkspaceEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling CreatedWorkspaceEvent for WorkspaceId: {WorkspaceId}, UserId: {UserId}", notification.workspaceId, notification.userId);

        var workspaceMember = WorkspaceMember.CreateOwner(notification.userId, notification.workspaceId, notification.userId);
        await _unitOfWork.Set<WorkspaceMember>().AddAsync(workspaceMember);
        // create a dashboard with default layout
        return;
    }
}
