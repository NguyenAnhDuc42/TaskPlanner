using Application.Interfaces.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Helpers;

public static class WorkflowHelper
{
    /// <summary>
    /// Resolves the active workflow for a target layer, bubbling up from Folder -> Space -> Workspace.
    /// </summary>
    public static async Task<Workflow> GetActiveWorkflow(IDataBase db, Guid workspaceId, Guid? spaceId, Guid? folderId, CancellationToken ct)
    {
        Guid? activeWorkflowId = null;

        // 1. Check Folder Level
        if (folderId.HasValue)
        {
            activeWorkflowId = await db.Folders
                .ById(folderId.Value)
                .Select(f => f.WorkflowId)
                .FirstOrDefaultAsync(ct);
        }

        // 2. Bubble up to Space Level
        if (activeWorkflowId == null && spaceId.HasValue)
        {
            activeWorkflowId = await db.Spaces
                .ById(spaceId.Value)
                .Select(s => s.WorkflowId)
                .FirstOrDefaultAsync(ct);
        }

        // 3. Resolve the Workflow Entity (or default to Workspace)
        Workflow? workflow = null;

        if (activeWorkflowId.HasValue)
        {
            workflow = await db.Workflows
                .Include(w => w.Statuses)
                .FirstOrDefaultAsync(w => w.Id == activeWorkflowId, ct);
        }

        // Fallback: Default Workspace Workflow (where SpaceId and FolderId are null)
        if (workflow == null)
        {
            workflow = await db.Workflows
                .Include(w => w.Statuses)
                .FirstOrDefaultAsync(w => w.ProjectWorkspaceId == workspaceId && 
                                          w.SpaceId == null && 
                                          w.FolderId == null, ct);
        }

        return workflow ?? throw new InvalidOperationException("Default workspace workflow not found. Please ensure workspace seeding is complete.");
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
