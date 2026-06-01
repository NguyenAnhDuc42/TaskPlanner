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
        if (!request.HasAnyMoves) 
            return Result.Success();

        if (context.CurrentMember.Role > Role.Member) 
            return Result.Failure(MemberError.DontHavePermission);

        // Validate ownership + referential integrity before touching the DB
        var validator = new BatchMoveValidator(db, context.workspaceId);

        if (request.Folders?.Any() ?? false)
        {
            var folderValidation = await validator.ValidateFolderMovesAsync(request.Folders, ct);
            if (folderValidation.IsFailure) return folderValidation;
        }

        if (request.Tasks?.Any() ?? false)
        {
            var taskValidation = await validator.ValidateTaskMovesAsync(request.Tasks, ct);
            if (taskValidation.IsFailure) return taskValidation;
        }

        var updatedSpaces = new List<SpaceRecord>();
        var updatedFolders = new List<FolderRecord>();
        var updatedTasks = new List<TaskRecord>();

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            // SPACES: Reorder only (spaces can't move between workspaces)
            if (request.Spaces?.Any() ?? false)
            {
                foreach (var move in request.Spaces)
                {
                    await db.ProjectSpaces
                        .Where(s => s.Id == move.ItemId && s.ProjectWorkspaceId == context.workspaceId)
                        .ExecuteUpdateAsync(u => u
                            .SetProperty(s => s.OrderKey, move.NewOrderKey)
                            .SetProperty(s => s.UpdatedAt, DateTimeOffset.UtcNow), ct);
                }
            }

            // FOLDERS: Move to new Space + reorder (no cascade — tasks own their space)
            if (request.Folders?.Any() ?? false)
            {
                foreach (var move in request.Folders)
                {
                    await db.ProjectFolders
                        .Where(f => f.Id == move.ItemId)
                        .ExecuteUpdateAsync(u => u
                            .SetProperty(f => f.ProjectSpaceId, f => move.TargetParentId ?? f.ProjectSpaceId)
                            .SetProperty(f => f.OrderKey, move.NewOrderKey)
                            .SetProperty(f => f.UpdatedAt, DateTimeOffset.UtcNow), ct);
                }
            }

            // TASKS: Move with explicit SpaceId + optional FolderId — no server-side parent resolution needed
            if (request.Tasks?.Any() ?? false)
            {
                foreach (var move in request.Tasks)
                {
                    await db.ProjectTasks
                        .Where(t => t.Id == move.ItemId)
                        .ExecuteUpdateAsync(u => u
                            .SetProperty(t => t.ProjectSpaceId, move.TargetSpaceId)
                            .SetProperty(t => t.ProjectFolderId, move.TargetFolderId)
                            .SetProperty(t => t.OrderKey, move.NewOrderKey)
                            .SetProperty(t => t.UpdatedAt, DateTimeOffset.UtcNow), ct);
                }
            }

            // Fetch updated records for realtime notification
            if (request.Spaces?.Any() ?? false)
            {
                var spaceIds = request.Spaces.Select(s => s.ItemId).ToList();
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

            if (request.Folders?.Any() ?? false)
            {
                var folderIds = request.Folders.Select(f => f.ItemId).ToList();
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

            if (request.Tasks?.Any() ?? false)
            {
                var taskIds = request.Tasks.Select(t => t.ItemId).ToList();
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
