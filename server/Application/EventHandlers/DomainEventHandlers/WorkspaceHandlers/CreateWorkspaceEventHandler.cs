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
        logger.LogInformation("Seeding initial hierarchy for WorkspaceId: {WorkspaceId}", notification.workspaceId);

        // Removed: Owner member is now handled directly in the ProjectWorkspace.Create factory method.
        
        var statuses = await SeedWorkflowAndStatuses(notification, cancellationToken);
        var space = await SeedWelcomeSpace(notification, cancellationToken);
        var folder = await SeedGettingStartedFolder(notification, space.Id, cancellationToken);
        
        await SeedInitialTasks(notification, space.Id, folder.Id, statuses, cancellationToken);

        // STAGE 2 Notification: Background seeding is complete
        await realtime.NotifyUserAsync(notification.userId, "WorkspaceReady", new { WorkspaceId = notification.workspaceId }, cancellationToken);
    }

    private async Task<ProjectSpace> SeedWelcomeSpace(CreatedWorkspaceEvent notification, CancellationToken cancellationToken)
    {
        var space = ProjectSpace.Create(
            notification.workspaceId,
            "Welcome Space",
            "welcome-space",
            "Initial space for your project.",
            null,
            isPrivate: false,
            creatorId: notification.userId,
            orderKey: FractionalIndex.Start()
        );
        await db.Spaces.AddAsync(space, cancellationToken);
        return space;
    }

    private async Task<ProjectFolder> SeedGettingStartedFolder(CreatedWorkspaceEvent notification, Guid spaceId, CancellationToken cancellationToken)
    {
        var folder = ProjectFolder.Create(
            notification.workspaceId,
            spaceId,
            "Getting Started",
            "getting-started",
            "Initial folder for your tasks.",
            FractionalIndex.Start(),
            false,
            notification.userId,
            null
        );
        await db.Folders.AddAsync(folder, cancellationToken);
        return folder;
    }

    private async Task<List<Status>> SeedWorkflowAndStatuses(CreatedWorkspaceEvent notification, CancellationToken cancellationToken)
    {
        var workflow = Workflow.Create(notification.workspaceId, "Default Workflow", null, notification.userId);
        await db.Workflows.AddAsync(workflow, cancellationToken);
        
        var statuses = Status.CreateStarterSet(notification.workspaceId, workflow.Id, notification.userId);
        await db.Statuses.AddRangeAsync(statuses, cancellationToken);
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
            slug: "explore-hierarchy",
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
            slug: "standalone-task",
            description: "This task lives directly under the space.",
            customization: null,
            creatorId: notification.userId,
            statusId: firstStatus.Id,
            orderKey: FractionalIndex.After(FractionalIndex.Start())
        );

        await db.Tasks.AddAsync(folderTask, cancellationToken);
        await db.Tasks.AddAsync(spaceTask, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}
