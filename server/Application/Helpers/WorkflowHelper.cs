using Application.Interfaces.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Helpers;

public static class WorkflowHelper
{

    public static async Task<Workflow> GetActiveWorkflow(IDataBase db, Guid workspaceId, Guid? spaceId, Guid? folderId, CancellationToken ct)
    {
        if (folderId.HasValue)
        {
            var workflow = await db.Workflows
                .Include(w => w.Statuses)
                .FirstOrDefaultAsync(w => w.ProjectFolderId == folderId.Value, ct);
            
            if (workflow != null) return workflow;
            
            var folder = await db.Folders.ById(folderId.Value).FirstOrDefaultAsync(ct);
            spaceId ??= folder?.ProjectSpaceId;
        }

        if (spaceId.HasValue)
        {
            var workflow = await db.Workflows
                .Include(w => w.Statuses)
                .FirstOrDefaultAsync(w => w.ProjectSpaceId == spaceId.Value && w.ProjectFolderId == null, ct);
            
            if (workflow != null) return workflow;
        }

        throw new InvalidOperationException("No active workflow found for this layer. Please ensure the Space has a workflow.");
    }

    public static async Task<List<Status>> GetActiveStatuses(IDataBase db, Guid workspaceId, Guid? spaceId, Guid? folderId, CancellationToken ct)
    {
        var workflow = await GetActiveWorkflow(db, workspaceId, spaceId, folderId, ct);
        return workflow.Statuses.OrderBy(s => s.Category).ThenBy(s => s.Name).ToList();
    }
}
