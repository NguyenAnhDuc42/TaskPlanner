using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Application;

public class BatchMoveValidator(TaskPlanDbContext db, Guid workspaceId)
{
    /// <summary>
    /// Validates folder moves:
    /// - All folders exist in this workspace (via their ProjectSpaceId)
    /// - All target spaces (TargetParentId) exist in this workspace
    /// </summary>
    public async Task<Result> ValidateFolderMovesAsync(List<MoveFolderValue> moves, CancellationToken ct)
    {
        if (!moves.Any()) return Result.Success();

        // Folders must exist under a space that belongs to this workspace
        var folderIds = moves.Select(m => m.ItemId).Distinct().ToList();
        var foundCount = await db.ProjectFolders
            .AsNoTracking()
            .Where(f => folderIds.Contains(f.Id) &&
                db.ProjectSpaces.Any(s => s.Id == f.ProjectSpaceId && s.ProjectWorkspaceId == workspaceId))
            .CountAsync(ct);

        if (foundCount != folderIds.Count)
            return Result.Failure(Error.NotFound("Folder.BatchMoveNotFound",
                "One or more folders were not found in this workspace."));

        // Target spaces (when provided) must exist under this workspace
        var targetSpaceIds = moves
            .Where(m => m.TargetParentId.HasValue)
            .Select(m => m.TargetParentId!.Value)
            .Distinct()
            .ToList();

        if (targetSpaceIds.Any())
        {
            var spaceCount = await db.ProjectSpaces
                .AsNoTracking()
                .Where(s => targetSpaceIds.Contains(s.Id) && s.ProjectWorkspaceId == workspaceId)
                .CountAsync(ct);

            if (spaceCount != targetSpaceIds.Count)
                return Result.Failure(Error.NotFound("Space.BatchMoveTargetNotFound",
                    "One or more target spaces were not found in this workspace."));
        }

        return Result.Success();
    }

    /// <summary>
    /// Validates task moves:
    /// - All tasks exist in this workspace (via their ProjectSpaceId — NOT ProjectFolder which can be null)
    /// - All target spaces exist in this workspace
    /// - If TargetFolderId is provided: folder must exist AND belong to TargetSpaceId
    /// </summary>
    public async Task<Result> ValidateTaskMovesAsync(List<MoveTaskValue> moves, CancellationToken ct)
    {
        if (!moves.Any()) return Result.Success();

        // Tasks must exist under a space that belongs to this workspace
        // Use ProjectSpaceId scalar — NOT ProjectFolder navigation (can be null for tasks directly in a space)
        var taskIds = moves.Select(m => m.ItemId).Distinct().ToList();
        var foundCount = await db.ProjectTasks
            .AsNoTracking()
            .Where(t => taskIds.Contains(t.Id) &&
                db.ProjectSpaces.Any(s => s.Id == t.ProjectSpaceId && s.ProjectWorkspaceId == workspaceId))
            .CountAsync(ct);

        if (foundCount != taskIds.Count)
            return Result.Failure(Error.NotFound("Task.BatchMoveNotFound",
                "One or more tasks were not found in this workspace."));

        // All target spaces must exist under this workspace
        var targetSpaceIds = moves.Select(m => m.TargetSpaceId).Distinct().ToList();
        var spaceCount = await db.ProjectSpaces
            .AsNoTracking()
            .Where(s => targetSpaceIds.Contains(s.Id) && s.ProjectWorkspaceId == workspaceId)
            .CountAsync(ct);

        if (spaceCount != targetSpaceIds.Count)
            return Result.Failure(Error.NotFound("Space.BatchMoveTargetNotFound",
                "One or more target spaces were not found in this workspace."));

        // If TargetFolderId is provided: folder must exist AND be in the declared TargetSpaceId
        var folderMoves = moves.Where(m => m.TargetFolderId.HasValue).ToList();
        if (folderMoves.Any())
        {
            var targetFolderIds = folderMoves
                .Select(m => m.TargetFolderId!.Value)
                .Distinct()
                .ToList();

            var folders = await db.ProjectFolders
                .AsNoTracking()
                .Where(f => targetFolderIds.Contains(f.Id))
                .Select(f => new { f.Id, f.ProjectSpaceId })
                .ToDictionaryAsync(f => f.Id, ct);

            var hasMismatch = folderMoves.Any(m =>
                !folders.ContainsKey(m.TargetFolderId!.Value) ||
                folders[m.TargetFolderId!.Value].ProjectSpaceId != m.TargetSpaceId);

            if (hasMismatch)
                return Result.Failure(Error.Validation("Folder.BatchMoveNotInSpace",
                    "One or more target folders do not belong to the declared target space."));
        }

        return Result.Success();
    }
}
