using Application.Interfaces.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Helpers;

public static class WorkflowHelper
{
    /// <summary>
    /// Resolves the active workflow for a target layer, bubbling up from Folder -> Space -> Workspace based on inheritance rules.
    /// </summary>
    public static async Task<Workflow> GetActiveWorkflow(IDataBase db, Guid workspaceId, Guid? spaceId, Guid? folderId, CancellationToken ct)
    {
        // 1. Check Folder Level (if not inheriting)
        if (folderId.HasValue)
        {
            var folder = await db.Folders.ById(folderId.Value).FirstOrDefaultAsync(ct);
            if (folder != null && !folder.IsInheritingWorkflow)
            {
                var workflow = await db.Workflows
                    .Include(w => w.Statuses)
                    .FirstOrDefaultAsync(w => w.ProjectFolderId == folder.Id, ct);
                
                if (workflow != null) return workflow;
            }
            
            // If inheriting or no specific workflow found, use the spaceId from the folder
            spaceId ??= folder?.ProjectSpaceId;
        }

        // 2. Check Space Level (if not inheriting)
        if (spaceId.HasValue)
        {
            var space = await db.Spaces.ById(spaceId.Value).FirstOrDefaultAsync(ct);
            if (space != null && !space.IsInheritingWorkflow)
            {
                var workflow = await db.Workflows
                    .Include(w => w.Statuses)
                    .FirstOrDefaultAsync(w => w.ProjectSpaceId == space.Id && w.ProjectFolderId == null, ct);
                
                if (workflow != null) return workflow;
            }
        }

        // 3. Fallback: Default Workspace Workflow (where ProjectSpaceId and ProjectFolderId are null)
        var workspaceWorkflow = await db.Workflows
            .Include(w => w.Statuses)
            .FirstOrDefaultAsync(w => w.ProjectWorkspaceId == workspaceId && 
                                      w.ProjectSpaceId == null && 
                                      w.ProjectFolderId == null, ct);

        return workspaceWorkflow ?? throw new InvalidOperationException("Default workspace workflow not found. Please ensure workspace seeding is complete.");
    }

    /// <summary>
    /// Returns the list of available statuses for a target layer, resolving inheritance.
    /// </summary>
    public static async Task<List<Status>> GetActiveStatuses(IDataBase db, Guid workspaceId, Guid? spaceId, Guid? folderId, CancellationToken ct)
    {
        var workflow = await GetActiveWorkflow(db, workspaceId, spaceId, folderId, ct);
        return workflow.Statuses.OrderBy(s => s.Category).ThenBy(s => s.Name).ToList();
    }
}
