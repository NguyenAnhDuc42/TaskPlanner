using Microsoft.EntityFrameworkCore;

namespace Application;

public static class WorkflowHelper
{
    public static async Task<Workflow?> GetActiveWorkflow(
        TaskPlanDbContext db,
        Guid workspaceId,
        Guid? spaceId,
        Guid? folderId,
        CancellationToken cancellationToken)
    {
        if (folderId.HasValue)
            return await db.Workflows
                .Include(w => w.Statuses)
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.ProjectWorkspaceId == workspaceId && w.ProjectFolderId == folderId.Value, cancellationToken);

        if (spaceId.HasValue)
            return await db.Workflows
                .Include(w => w.Statuses)
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.ProjectWorkspaceId == workspaceId && w.ProjectSpaceId == spaceId.Value && w.ProjectFolderId == null, cancellationToken);

        return null;
    }

    public static async Task<List<Status>?> GetActiveStatuses(
        TaskPlanDbContext db,
        Guid workspaceId,
        Guid? spaceId,
        Guid? folderId,
        CancellationToken cancellationToken)
    {
        var workflow = await GetActiveWorkflow(db, workspaceId, spaceId, folderId, cancellationToken);
        if (workflow is null) return null;

        var statuses = workflow.Statuses
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .ToList();

        return statuses.Count > 0 ? statuses : null;
    }
}

