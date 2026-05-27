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

            foreach (var move in taskMoves)
            {
                Guid? resolvedSpaceId = null;
                Guid? resolvedFolderId = null;
                bool hasTarget = false;

                if (move.TargetParentId.HasValue)
                {
                    var targetGuid = move.TargetParentId.Value;
                    var targetInfo = await db.ProjectSpaces
                        .Where(s => s.Id == targetGuid)
                        .Select(s => new { Id = s.Id, Type = "ProjectSpace" })
                        .Union(db.ProjectFolders.Where(f => f.Id == targetGuid).Select(f => new { Id = f.ProjectSpaceId, Type = "ProjectFolder" }))
                        .FirstOrDefaultAsync(ct);

                    if (targetInfo != null)
                    {
                        hasTarget = true;
                        if (targetInfo.Type == "ProjectSpace") resolvedSpaceId = targetGuid;
                        else { resolvedFolderId = targetGuid; resolvedSpaceId = targetInfo.Id; }
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

            if (spaceMoves.Any())
            {
                var spaceIds = spaceMoves.Select(m => m.ItemId).ToList();
                updatedSpaces = await db.ProjectSpaces
                    .AsNoTracking()
                    .Where(s => spaceIds.Contains(s.Id))
                    .Select(s => new SpaceRecord
                    {
                        Id = s.Id,
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
                        Name = f.Name,
                        ParentId = f.ProjectSpaceId,
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
