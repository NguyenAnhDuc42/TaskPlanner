using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Api;

public class WorkspaceService(
    IServiceScopeFactory scopeFactory,
    ILogger<WorkspaceService> logger
)
{
    public void InitializeInBackground(Guid workspaceId, Guid creatorId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TaskPlanDbContext>();
                var realtime = scope.ServiceProvider.GetRequiredService<RealtimeService>();

                var workspace = await db.ProjectWorkspaces
                    .Include(w => w.Members)
                    .FirstOrDefaultAsync(w => w.Id == workspaceId);

                if (workspace == null || workspace.IsInitialized)
                    return;

                // 2. Create Default Space — defaultDocumentId is just a grouping key for the
                // space's DocumentBlocks now (see Document entity removal); no Document row backs it.
                var space = ProjectSpace.CreateDefault(workspace.Id, Guid.NewGuid(), creatorId);
                await db.ProjectSpaces.AddAsync(space);

                var spaceStatuses = Status.CreateSpaceStarterSet(workspace.Id, space.Id, creatorId);
                await db.Statuses.AddRangeAsync(spaceStatuses);


                // 3. Create Default Folder
                var folder = ProjectFolder.CreateDefault(workspace.Id, space.Id, creatorId);
                await db.ProjectFolders.AddAsync(folder);


                // 4. Create Starter Tasks
                var firstStatus = spaceStatuses.First();

                var tasks = ProjectTask.CreateDefaults(workspace.Id, space.Id, folder.Id, firstStatus.Id, creatorId, Guid.NewGuid(), Guid.NewGuid());
                await db.ProjectTasks.AddRangeAsync(tasks);

                // 5. Finalize
                workspace.MarkAsInitialized();
                await db.SaveChangesAsync();

                // 6. Notify User
                await realtime.NotifyUserAsync(creatorId, "WorkspaceReady", new { WorkspaceId = workspace.Id });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize workspace {WorkspaceId}", workspaceId);
            }
        });
    }
}



