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

        // 3. Create Example Space
        var exampleSpace = ProjectSpace.Create(
            notification.workspaceId,
            "Welcome Space",
            "This is your first space. You can add more spaces, folders, and lists to organize your work.",
            null, // default customization
            isPrivate: true,
            creatorId: notification.userId,
            orderKey: 10_000_000
        );
        await _unitOfWork.Set<ProjectSpace>().AddAsync(exampleSpace, cancellationToken);

        // 4. Create Main Workflow for the Space
        var mainWorkflow = Workflow.Create(
            exampleSpace.Id, 
            "Standard Workflow", 
            "The primary task pipeline for this space.", 
            notification.userId);
        await _unitOfWork.Set<Workflow>().AddAsync(mainWorkflow, cancellationToken);

        // 5. Create Default Statuses linked to the Workflow
        var defaultStatuses = Status.CreateDefaultStatuses(mainWorkflow.Id, notification.userId);
        await _unitOfWork.Set<Status>().AddRangeAsync(defaultStatuses, cancellationToken);
    }
}
