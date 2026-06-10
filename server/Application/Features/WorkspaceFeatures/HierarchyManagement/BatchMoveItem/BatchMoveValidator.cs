using Microsoft.EntityFrameworkCore;

namespace Application;

public class BatchMoveValidator(TaskPlanDbContext db, Guid workspaceId)
{
    public async Task<Result> ValidateFolderMovesAsync(List<MoveFolderValue> moves, CancellationToken cancellationToken)
    {
        if (!moves.Any()) return Result.Success();

        var folderIds = moves.Select(m => m.ItemId).Distinct().ToList();
        var foundCount = await db.ProjectFolders
            .AsNoTracking()
            .Where(f => folderIds.Contains(f.Id) &&
                db.ProjectSpaces.Any(s => s.Id == f.ProjectSpaceId && s.ProjectWorkspaceId == workspaceId))
            .CountAsync(cancellationToken);

        if (foundCount != folderIds.Count)
            return Result.Failure(Error.NotFound("Folder.BatchMoveNotFound",
                "One or more folders were not found in this workspace."));

        var targetSpaceIds = moves
            .Where(m => m.TargetParentId.HasValue)
            .Select(m => m.TargetParentId!.Value)
            .Distinct()
            .ToList();

        if (targetSpaceIds.Any())
        {
            var targetSpaces = await db.ProjectSpaces
                .AsNoTracking()
                .Where(s => targetSpaceIds.Contains(s.Id) && s.ProjectWorkspaceId == workspaceId)
                .Select(s => new { s.Id, s.IsPrivate })
                .ToListAsync(cancellationToken);

            if (targetSpaces.Count != targetSpaceIds.Count)
                return Result.Failure(Error.NotFound("Space.BatchMoveTargetNotFound",
                    "One or more target spaces were not found in this workspace."));

            var privateTargetSpaceIds = targetSpaces
                .Where(s => s.IsPrivate)
                .Select(s => s.Id)
                .ToHashSet();

            if (privateTargetSpaceIds.Any())
            {
                var movesIntoPrivate = moves
                    .Where(m => m.TargetParentId.HasValue && privateTargetSpaceIds.Contains(m.TargetParentId.Value))
                    .ToList();

                var movingFolderIds = movesIntoPrivate.Select(m => m.ItemId).Distinct().ToList();
                var sourceFolderSpaces = await db.ProjectFolders
                    .AsNoTracking()
                    .Where(f => movingFolderIds.Contains(f.Id))
                    .Select(f => new { f.Id, f.ProjectSpaceId })
                    .ToListAsync(cancellationToken);

                var sourceMap = sourceFolderSpaces.ToDictionary(f => f.Id, f => f.ProjectSpaceId);

                var isCrossSpacePrivateMove = movesIntoPrivate.Any(m =>
                    !sourceMap.TryGetValue(m.ItemId, out var srcSpaceId) ||
                    srcSpaceId != m.TargetParentId);

                if (isCrossSpacePrivateMove)
                    return Result.Failure(Error.Validation("Space.BatchMoveTargetPrivate",
                        "Cannot move items to a private space."));
            }
        }

        return Result.Success();
    }

    public async Task<Result> ValidateTaskMovesAsync(List<MoveTaskValue> moves, CancellationToken cancellationToken)
    {
        if (!moves.Any()) return Result.Success();
        var taskIds = moves.Select(m => m.ItemId).Distinct().ToList();
        var foundCount = await db.ProjectTasks
            .AsNoTracking()
            .Where(t => taskIds.Contains(t.Id) &&
                db.ProjectSpaces.Any(s => s.Id == t.ProjectSpaceId && s.ProjectWorkspaceId == workspaceId))
            .CountAsync(cancellationToken);

        if (foundCount != taskIds.Count)
            return Result.Failure(Error.NotFound("Task.BatchMoveNotFound",
                "One or more tasks were not found in this workspace."));
        var targetSpaceIds = moves.Select(m => m.TargetSpaceId).Distinct().ToList();

        var targetSpaces = await db.ProjectSpaces
            .AsNoTracking()
            .Where(s => targetSpaceIds.Contains(s.Id) && s.ProjectWorkspaceId == workspaceId)
            .Select(s => new { s.Id, s.IsPrivate })
            .ToListAsync(cancellationToken);

        if (targetSpaces.Count != targetSpaceIds.Count)
            return Result.Failure(Error.NotFound("Space.BatchMoveTargetNotFound",
                "One or more target spaces were not found in this workspace."));

        // Only block cross-space moves into a private space.
        // Reordering within the same private space is allowed.
        var privateTargetSpaceIds = targetSpaces
            .Where(s => s.IsPrivate)
            .Select(s => s.Id)
            .ToHashSet();

        if (privateTargetSpaceIds.Any())
        {
            var movesIntoPrivate = moves
                .Where(m => privateTargetSpaceIds.Contains(m.TargetSpaceId))
                .ToList();

            var movingTaskIds = movesIntoPrivate.Select(m => m.ItemId).Distinct().ToList();
            var sourceTaskSpaces = await db.ProjectTasks
                .AsNoTracking()
                .Where(t => movingTaskIds.Contains(t.Id))
                .Select(t => new { t.Id, t.ProjectSpaceId })
                .ToListAsync(cancellationToken);

            var sourceMap = sourceTaskSpaces.ToDictionary(t => t.Id, t => t.ProjectSpaceId);

            var isCrossSpacePrivateMove = movesIntoPrivate.Any(m =>
                !sourceMap.TryGetValue(m.ItemId, out var srcSpaceId) ||
                srcSpaceId != m.TargetSpaceId);

            if (isCrossSpacePrivateMove)
                return Result.Failure(Error.Validation("Space.BatchMoveTargetPrivate",
                    "Cannot move items to a private space."));
        }

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
                .ToDictionaryAsync(f => f.Id, cancellationToken);

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
