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

        if (context.CurrentMember.Role != Role.Admin && context.CurrentMember.Role != Role.Owner)
            return Result.Failure(MemberError.DontHavePermission);

        var folderValTask = request.Folders.Count > 0
            ? new BatchMoveValidator(db, context.workspaceId).ValidateFolderMovesAsync(request.Folders, ct)
            : Task.FromResult(Result.Success());

        var taskValTask = request.Tasks.Count > 0
            ? new BatchMoveValidator(db, context.workspaceId).ValidateTaskMovesAsync(request.Tasks, ct)
            : Task.FromResult(Result.Success());

        await Task.WhenAll(folderValTask, taskValTask);

        var folderValidation = await folderValTask;
        if (folderValidation.IsFailure) return folderValidation;

        var taskValidation = await taskValTask;
        if (taskValidation.IsFailure) return taskValidation;

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            await ApplyMovesAsync(request, ct);
            return Result.Success();
        }, ct);

        if (result.IsFailure)
            return result;

        var packet = await FetchUpdatedRecordsAsync(request, ct);

        if (packet.HasAny)
            await realtime.NotifyEntitiesUpdatedAsync(context.workspaceId, packet, ct);

        return Result.Success();
    }


    private async Task ApplyMovesAsync(BatchMoveItemCommand request, CancellationToken ct)
    {
        if (request.Spaces.Count > 0)
        {
            var orderMap = request.Spaces.ToDictionary(s => s.ItemId, s => s.NewOrderKey);

            await db.ProjectSpaces
                .Where(s => orderMap.Keys.Contains(s.Id) && s.ProjectWorkspaceId == context.workspaceId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(s => s.OrderKey, s => orderMap[s.Id])
                    .SetProperty(s => s.UpdatedAt, DateTimeOffset.UtcNow), ct);
        }

        if (request.Folders.Count > 0)
        {
            // Split into two groups: folders that are being re-parented vs reorder-only
            var withParentChange = request.Folders.Where(f => f.TargetParentId.HasValue).ToList();
            var reorderOnly      = request.Folders.Where(f => !f.TargetParentId.HasValue).ToList();

            if (withParentChange.Count > 0)
            {
                // Group by target space so we still get one UPDATE per distinct target
                foreach (var group in withParentChange.GroupBy(f => f.TargetParentId!.Value))
                {
                    var ids      = group.Select(f => f.ItemId).ToList();
                    var orderMap = group.ToDictionary(f => f.ItemId, f => f.NewOrderKey);
                    var spaceId  = group.Key;

                    await db.ProjectFolders
                        .Where(f => ids.Contains(f.Id))
                        .ExecuteUpdateAsync(u => u
                            .SetProperty(f => f.ProjectSpaceId, spaceId)
                            .SetProperty(f => f.OrderKey, f => orderMap[f.Id])
                            .SetProperty(f => f.UpdatedAt, DateTimeOffset.UtcNow), ct);
                }
            }

            if (reorderOnly.Count > 0)
            {
                var ids      = reorderOnly.Select(f => f.ItemId).ToList();
                var orderMap = reorderOnly.ToDictionary(f => f.ItemId, f => f.NewOrderKey);

                await db.ProjectFolders
                    .Where(f => ids.Contains(f.Id))
                    .ExecuteUpdateAsync(u => u
                        .SetProperty(f => f.OrderKey, f => orderMap[f.Id])
                        .SetProperty(f => f.UpdatedAt, DateTimeOffset.UtcNow), ct);
            }
        }

        if (request.Tasks.Count > 0)
        {
            // Group by (TargetSpaceId, TargetFolderId) so each group is one UPDATE
            foreach (var group in request.Tasks.GroupBy(t => (t.TargetSpaceId, t.TargetFolderId)))
            {
                var ids      = group.Select(t => t.ItemId).ToList();
                var orderMap = group.ToDictionary(t => t.ItemId, t => t.NewOrderKey);
                var spaceId  = group.Key.TargetSpaceId;
                var folderId = group.Key.TargetFolderId;

                await db.ProjectTasks
                    .Where(t => ids.Contains(t.Id))
                    .ExecuteUpdateAsync(u => u
                        .SetProperty(t => t.ProjectSpaceId, spaceId)
                        .SetProperty(t => t.ProjectFolderId, folderId)
                        .SetProperty(t => t.OrderKey, t => orderMap[t.Id])
                        .SetProperty(t => t.UpdatedAt, DateTimeOffset.UtcNow), ct);
            }
        }
    }

    private async Task<EntityBatchUpdate> FetchUpdatedRecordsAsync(BatchMoveItemCommand request, CancellationToken ct)
    {
        var spacesTask = request.Spaces.Count > 0
            ? FetchSpacesAsync(request.Spaces.Select(s => s.ItemId).ToList(), ct)
            : Task.FromResult<List<SpaceRecord>>([]);

        var foldersTask = request.Folders.Count > 0
            ? FetchFoldersAsync(request.Folders.Select(f => f.ItemId).ToList(), ct)
            : Task.FromResult<List<FolderRecord>>([]);

        var tasksTask = request.Tasks.Count > 0
            ? FetchTasksAsync(request.Tasks.Select(t => t.ItemId).ToList(), ct)
            : Task.FromResult<List<TaskRecord>>([]);

        await Task.WhenAll(spacesTask, foldersTask, tasksTask);

        return new EntityBatchUpdate
        {
            Spaces  = (await spacesTask).NullIfEmpty(),
            Folders = (await foldersTask).NullIfEmpty(),
            Tasks   = (await tasksTask).NullIfEmpty()
        };
    }

    private Task<List<SpaceRecord>> FetchSpacesAsync(List<Guid> ids, CancellationToken ct) =>
        db.ProjectSpaces
            .AsNoTracking()
            .Where(s => ids.Contains(s.Id))
            .Select(s => new SpaceRecord
            {
                Id          = s.Id,
                WorkspaceId = s.ProjectWorkspaceId,
                Name        = s.Name,
                Color       = s.Color,
                Icon        = s.Icon,
                IsPrivate   = s.IsPrivate,
                OrderKey    = s.OrderKey
            })
            .ToListAsync(ct);

    private Task<List<FolderRecord>> FetchFoldersAsync(List<Guid> ids, CancellationToken ct) =>
        db.ProjectFolders
            .AsNoTracking()
            .Where(f => ids.Contains(f.Id))
            .Select(f => new FolderRecord
            {
                Id          = f.Id,
                WorkspaceId = context.workspaceId,
                SpaceId     = f.ProjectSpaceId,
                Name        = f.Name,
                OrderKey    = f.OrderKey,
                Icon        = f.Icon,
                Color       = f.Color
            })
            .ToListAsync(ct);

    private Task<List<TaskRecord>> FetchTasksAsync(List<Guid> ids, CancellationToken ct) =>
        db.ProjectTasks
            .AsNoTracking()
            .Where(t => ids.Contains(t.Id))
            .Select(t => new TaskRecord
            {
                Id              = t.Id,
                WorkspaceId     = context.workspaceId,
                Name            = t.Name,
                StatusId        = t.StatusId,
                Priority        = t.Priority,
                StartDate       = t.StartDate,
                DueDate         = t.DueDate,
                OrderKey        = t.OrderKey,
                Icon            = t.Icon,
                Color           = t.Color,
                ProjectSpaceId  = t.ProjectSpaceId,
                ProjectFolderId = t.ProjectFolderId
            })
            .ToListAsync(ct);
}

public static class ListExtensions
{
    public static List<T>? NullIfEmpty<T>(this List<T> list) =>
        list.Count > 0 ? list : null;
}
