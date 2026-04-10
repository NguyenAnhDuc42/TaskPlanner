using Application.Interfaces.Data;
using Domain.Common;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Events.Workspace;
using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers.DomainEventHandlers.WorkspaceHandlers;

public class CreateWorkspaceEventHandler : IDomainEventHandler<CreatedWorkspaceEvent>
{
    private readonly ILogger<CreateWorkspaceEventHandler> _logger;
    private readonly IDataBase _db;
    
    public CreateWorkspaceEventHandler(ILogger<CreateWorkspaceEventHandler> logger, IDataBase db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task Handle(CreatedWorkspaceEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seeding initial hierarchy for WorkspaceId: {WorkspaceId}", notification.workspaceId);

        await CreateOwnerProfile(notification, cancellationToken);
        var statuses = await SeedWorkflowAndStatuses(notification, cancellationToken);
        var space = await SeedWelcomeSpace(notification, cancellationToken);
        var folder = await SeedGettingStartedFolder(notification, space.Id, cancellationToken);
        
        await SeedInitialTasks(notification, space.Id, folder.Id, statuses, cancellationToken);
    }

    private async Task CreateOwnerProfile(CreatedWorkspaceEvent notification, CancellationToken cancellationToken)
    {
        var workspaceMember = WorkspaceMember.CreateOwner(notification.userId, notification.workspaceId, notification.userId);
        await _db.Members.AddAsync(workspaceMember, cancellationToken);
    }

    private async Task<ProjectSpace> SeedWelcomeSpace(CreatedWorkspaceEvent notification, CancellationToken cancellationToken)
    {
        var space = ProjectSpace.Create(
            notification.workspaceId,
            "Welcome Space",
            "Initial space for your project.",
            null,
            isPrivate: false,
            creatorId: notification.userId,
            orderKey: FractionalIndex.Start()
        );
        await _db.Spaces.AddAsync(space, cancellationToken);
        return space;
    }

    private async Task<ProjectFolder> SeedGettingStartedFolder(CreatedWorkspaceEvent notification, Guid spaceId, CancellationToken cancellationToken)
    {
        var folder = ProjectFolder.Create(
            spaceId,
            "Getting Started",
            "#6366f1",
            "Folder",
            isPrivate: false,
            creatorId: notification.userId,
            orderKey: FractionalIndex.Start()
        );
        await _db.Folders.AddAsync(folder, cancellationToken);
        return folder;
    }

    private async Task<List<Status>> SeedWorkflowAndStatuses(CreatedWorkspaceEvent notification, CancellationToken cancellationToken)
    {
        var workflow = Workflow.Create(notification.workspaceId, "Default Workflow", null, notification.userId);
        await _db.Workflows.AddAsync(workflow, cancellationToken);
        
        var statuses = Status.CreateStarterSet(notification.workspaceId, workflow.Id, notification.userId);
        await _db.Statuses.AddRangeAsync(statuses, cancellationToken);
        return statuses;
    }

    private async Task SeedInitialTasks(CreatedWorkspaceEvent notification, Guid spaceId, Guid folderId, List<Status> statuses, CancellationToken cancellationToken)
    {
        var firstStatus = statuses.First(s => s.Category == Domain.Enums.StatusCategory.NotStarted);

        var folderTask = ProjectTask.Create(
            projectWorkspaceId: notification.workspaceId,
            projectSpaceId: spaceId,
            projectFolderId: folderId,
            name: "Explore the hierarchy",
            description: "Notice how this task is nested under the 'Getting Started' folder.",
            customization: null,
            creatorId: notification.userId,
            statusId: firstStatus.Id,
            orderKey: FractionalIndex.Start()
        );
        
        var spaceTask = ProjectTask.Create(
            projectWorkspaceId: notification.workspaceId,
            projectSpaceId: spaceId,
            projectFolderId: null,
            name: "Standalone Task",
            description: "This task lives directly under the space.",
            customization: null,
            creatorId: notification.userId,
            statusId: firstStatus.Id,
            orderKey: FractionalIndex.After(FractionalIndex.Start())
        );

        await _db.Tasks.AddAsync(folderTask, cancellationToken);
        await _db.Tasks.AddAsync(spaceTask, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
