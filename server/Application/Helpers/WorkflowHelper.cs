using Application.Common.Results;
using Application.Interfaces.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Helpers;

public static class WorkflowHelper
{
    public static async Task<Workflow?> GetActiveWorkflow(
        IDataBase db,
        Guid workspaceId,
        Guid? spaceId,
        Guid? folderId,
        CancellationToken ct)
    {
        if (folderId.HasValue)
            return await db.Workflows
                .Include(w => w.Statuses)
                .FirstOrDefaultAsync(w => w.ProjectWorkspaceId == workspaceId && w.ProjectFolderId == folderId.Value, ct);

        if (spaceId.HasValue)
            return await db.Workflows
                .Include(w => w.Statuses)
                .FirstOrDefaultAsync(w => w.ProjectWorkspaceId == workspaceId && w.ProjectSpaceId == spaceId.Value && w.ProjectFolderId == null, ct);

        return null;
    }

    public static async Task<List<Status>?> GetActiveStatuses(
        IDataBase db,
        Guid workspaceId,
        Guid? spaceId,
        Guid? folderId,
        CancellationToken ct)
    {
        var workflow = await GetActiveWorkflow(db, workspaceId, spaceId, folderId, ct);
        if (workflow is null) return null;

        var statuses = workflow.Statuses
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .ToList();

        return statuses.Count > 0 ? statuses : null;
    }
}