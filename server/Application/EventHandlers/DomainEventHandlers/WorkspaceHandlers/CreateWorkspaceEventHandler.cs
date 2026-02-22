using System;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
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

        // 1. Create Workspace Owner
        var workspaceMember = WorkspaceMember.CreateOwner(notification.userId, notification.workspaceId, notification.userId);
        await _unitOfWork.Set<WorkspaceMember>().AddAsync(workspaceMember, cancellationToken);

        // 2. Create Default Dashboard
        var dashboard = Dashboard.CreateWorkspaceDashboard(notification.workspaceId, notification.userId, "Overview", isMain: true);
        await _unitOfWork.Set<Dashboard>().AddAsync(dashboard, cancellationToken);

        // 3. Create Default Statuses
        var todoStatus = Status.Create(notification.workspaceId, Domain.Enums.RelationShip.EntityLayerType.ProjectWorkspace, 
            "To Do", "#87909e", Domain.Enums.StatusCategory.NotStarted, 0, notification.userId);
        
        var inProgressStatus = Status.Create(notification.workspaceId, Domain.Enums.RelationShip.EntityLayerType.ProjectWorkspace, 
            "In Progress", "#337ea9", Domain.Enums.StatusCategory.Active, 1, notification.userId);
        
        var doneStatus = Status.Create(notification.workspaceId, Domain.Enums.RelationShip.EntityLayerType.ProjectWorkspace, 
            "Done", "#209955", Domain.Enums.StatusCategory.Done, 2, notification.userId);
        doneStatus.SetDefault(true);

        await _unitOfWork.Set<Status>().AddRangeAsync(new[] { todoStatus, inProgressStatus, doneStatus }, cancellationToken);

        // 4. Create Example Space
        var exampleSpace = ProjectSpace.Create(
            notification.workspaceId,
            "Welcome Space",
            "This is your first space. You can add more spaces, folders, and lists to organize your work.",
            null, // default customization
            isPrivate: true,
            inheritStatus: true,
            creatorId: notification.userId,
            orderKey: 10_000_000
        );
        await _unitOfWork.Set<ProjectSpace>().AddAsync(exampleSpace, cancellationToken);
    }
}

