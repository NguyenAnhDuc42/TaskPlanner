using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Application;

public class BatchMoveItemHandler(
    TaskPlanDbContext db, 
    WorkspaceContext context, 
    RealtimeService realtime
) : ICommandHandler<BatchMoveItemCommand>
{
    public async Task<Result> Handle(BatchMoveItemCommand request, CancellationToken ct)
    {
        if (request.Moves == null || !request.Moves.Any()) 
            return Result.Success();

        if (context.CurrentMember.Role > Role.Member) 
            return Result.Failure(MemberError.DontHavePermission);

        var updatedSpaces = new List<SpaceRecord>();
        var updatedFolders = new List<FolderRecord>();
        var updatedTasks = new List<TaskRecord>();

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            var spaceMoves = request.Moves.Where(m => m.ItemType == EntityLayerType.ProjectSpace).OrderBy(m => m.NewOrderKey).ToList();
            var folderMoves = request.Moves.Where(m => m.ItemType == EntityLayerType.ProjectFolder).OrderBy(m => m.NewOrderKey).ToList();
            var taskMoves = request.Moves.Where(m => m.ItemType == EntityLayerType.ProjectTask).OrderBy(m => m.NewOrderKey).ToList();

            foreach (var move in spaceMoves)
            {
                await db.ProjectSpaces
                    .Where(s => s.Id == move.ItemId && s.ProjectWorkspaceId == context.workspaceId)
                    .ExecuteUpdateAsync(u => u
                        .SetProperty(s => s.OrderKey, move.NewOrderKey)
                        .SetProperty(s => s.UpdatedAt, DateTimeOffset.UtcNow), ct);
            }

            foreach (var move in folderMoves)
            {
                await db.ProjectFolders
                    .Where(f => f.Id == move.ItemId)
                    .ExecuteUpdateAsync(u => u
                        .SetProperty(f => f.ProjectSpaceId, f => move.TargetParentId ?? f.ProjectSpaceId)
                        .SetProperty(f => f.OrderKey, move.NewOrderKey)
                        .SetProperty(f => f.UpdatedAt, DateTimeOffset.UtcNow), ct);
            }

            if (taskMoves.Any())
            {
                // Pre-load all target parent info in ONE query instead of N UNION SELECTs
                var allTargetGuids = taskMoves
                    .Where(m => m.TargetParentId.HasValue)
                    .Select(m => m.TargetParentId!.Value)
                    .Distinct()
                    .ToList();

                var parentInfoMap = allTargetGuids.Any()
                    ? await db.ProjectSpaces
                        .Where(s => allTargetGuids.Contains(s.Id) && s.ProjectWorkspaceId == context.workspaceId)
                        .Select(s => new { s.Id, Type = "ProjectSpace", SpaceId = s.Id })
                        .Union(db.ProjectFolders
                            .Where(f => allTargetGuids.Contains(f.Id))
                            .Select(f => new { f.Id, Type = "ProjectFolder", SpaceId = f.ProjectSpaceId }))
                        .ToDictionaryAsync(x => x.Id, ct)
                    : [];

            foreach (var move in taskMoves)
            {
                Guid? resolvedSpaceId = null;
                Guid? resolvedFolderId = null;
                bool hasTarget = false;

                if (move.TargetParentId.HasValue)
                {
                    var targetGuid = move.TargetParentId.Value;
                    if (parentInfoMap.TryGetValue(targetGuid, out var info))
                    {
                        hasTarget = true;
                        if (info.Type == "ProjectSpace") resolvedSpaceId = targetGuid;
                        else { resolvedFolderId = targetGuid; resolvedSpaceId = info.SpaceId; }
                    }
                }

                await db.ProjectTasks
                    .Where(t => t.Id == move.ItemId)
                    .ExecuteUpdateAsync(u => u
                        .SetProperty(t => t.ProjectSpaceId, t => hasTarget ? resolvedSpaceId : t.ProjectSpaceId)
                        .SetProperty(t => t.ProjectFolderId, t => hasTarget ? resolvedFolderId : t.ProjectFolderId)
                        .SetProperty(t => t.OrderKey, move.NewOrderKey)
                        .SetProperty(t => t.UpdatedAt, DateTimeOffset.UtcNow), ct);
            }
            }

            if (spaceMoves.Any())
            {
                var spaceIds = spaceMoves.Select(m => m.ItemId).ToList();
                updatedSpaces = await db.ProjectSpaces
                    .AsNoTracking()
                    .Where(s => spaceIds.Contains(s.Id))
                    .Select(s => new SpaceRecord
                    {
                        Id = s.Id,
                        WorkspaceId = s.ProjectWorkspaceId,
                        Name = s.Name,
                        Color = s.Color,
                        Icon = s.Icon,
                        IsPrivate = s.IsPrivate,
                        OrderKey = s.OrderKey
                    }).ToListAsync(ct);
            }

            if (folderMoves.Any())
            {
                var folderIds = folderMoves.Select(m => m.ItemId).ToList();
                updatedFolders = await db.ProjectFolders
                    .AsNoTracking()
                    .Where(f => folderIds.Contains(f.Id))
                    .Select(f => new FolderRecord
                    {
                        Id = f.Id,
                        WorkspaceId = context.workspaceId,
                        SpaceId = f.ProjectSpaceId,
                        Name = f.Name,
                        OrderKey = f.OrderKey,
                        Icon = f.Icon,
                        Color = f.Color
                    }).ToListAsync(ct);
            }

            if (taskMoves.Any())
            {
                var taskIds = taskMoves.Select(m => m.ItemId).ToList();
                updatedTasks = await db.ProjectTasks
                    .AsNoTracking()
                    .Where(t => taskIds.Contains(t.Id))
                    .Select(t => new TaskRecord
                    {
                        Id = t.Id,
                        WorkspaceId = context.workspaceId,
                        Name = t.Name,
                        StatusId = t.StatusId,
                        Priority = t.Priority,
                        StartDate = t.StartDate,
                        DueDate = t.DueDate,
                        OrderKey = t.OrderKey,
                        Icon = t.Icon,
                        Color = t.Color,
                        ProjectSpaceId = t.ProjectSpaceId,
                        ProjectFolderId = t.ProjectFolderId
                    }).ToListAsync(ct);
            }

            return Result.Success();
        }, ct);

        if (result.IsSuccess)
        {
            var updatePacket = new EntityBatchUpdate
            {
                Spaces = updatedSpaces.Any() ? updatedSpaces : null,
                Folders = updatedFolders.Any() ? updatedFolders : null,
                Tasks = updatedTasks.Any() ? updatedTasks : null
            };

            await realtime.NotifyEntitiesUpdatedAsync(context.workspaceId, updatePacket, ct);
        }

        return result;
    }
}
