using Application.Helpers;
using Application.Interfaces.Data;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Features.WorkspaceFeatures;

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
                var db = scope.ServiceProvider.GetRequiredService<IDataBase>();
                var realtime = scope.ServiceProvider.GetRequiredService<IRealtimeService>();

                var workspace = await db.Workspaces
                    .Include(w => w.Members)
                    .FirstOrDefaultAsync(w => w.Id == workspaceId);

                if (workspace == null || workspace.IsInitialized)
                    return;

                // 2. Create Default Space
                var spaceDoc = Document.Create(workspace.Id, "Personal Space", creatorId);
                await db.Documents.AddAsync(spaceDoc);

                var space = ProjectSpace.CreateDefault(workspace.Id, spaceDoc.Id, creatorId);
                await db.Spaces.AddAsync(space);
                
                // Create Workflow for the Default Space!
                var spaceWorkflow = Workflow.Create(workspace.Id, "Personal Space Workflow", "", creatorId, projectSpaceId: space.Id);
                await db.Workflows.AddAsync(spaceWorkflow);
                
                var spaceStatuses = Status.CreateSpaceStarterSet(workspace.Id, spaceWorkflow.Id, creatorId);
                await db.Statuses.AddRangeAsync(spaceStatuses);

                db.ViewDefinitions.AddRange(
                    ViewDefinition.CreateDefaults(workspace.Id, space.Id, null, creatorId));

                // 3. Create Default Folder
                var folderDoc = Document.Create(workspace.Id, "Getting Started", creatorId);
                await db.Documents.AddAsync(folderDoc);

                var folder = ProjectFolder.CreateDefault(workspace.Id, space.Id, folderDoc.Id, creatorId);
                await db.Folders.AddAsync(folder);
                
                db.ViewDefinitions.AddRange(
                    ViewDefinition.CreateDefaults(workspace.Id, space.Id, folder.Id, creatorId));

                // 4. Create Starter Tasks
                var firstStatus = spaceStatuses.First(s => s.Category == StatusCategory.NotStarted);
                
                var exploreDoc = Document.Create(workspace.Id, "Explore the hierarchy", creatorId);
                var standaloneDoc = Document.Create(workspace.Id, "Standalone Task", creatorId);
                await db.Documents.AddRangeAsync(exploreDoc, standaloneDoc);

                var tasks = ProjectTask.CreateDefaults(workspace.Id, space.Id, folder.Id, firstStatus.Id, creatorId, exploreDoc.Id, standaloneDoc.Id);
                await db.Tasks.AddRangeAsync(tasks);

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
