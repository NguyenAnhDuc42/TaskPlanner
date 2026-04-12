using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Common;
using Domain.Entities.ProjectEntities;
using Domain.Enums.RelationShip;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.MoveItem;

public class MoveItemHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<MoveItemCommand>
{
    public async Task<Result> Handle(MoveItemCommand request, CancellationToken ct)
    {
        // Permission: Admin/Owner or the creator of the item
        if (context.CurrentMember.Role > Role.Admin) return Result.Failure(MemberError.DontHavePermission);

        var newOrderKey = request.NewOrderKey ?? await ResolveOrderKey(request, ct);

        Result result = request.ItemType switch
        {
            EntityLayerType.ProjectSpace => await MoveSpace(request.ItemId, newOrderKey, ct),
            EntityLayerType.ProjectFolder => await MoveFolder(request.ItemId, request.TargetParentId, newOrderKey, ct),
            EntityLayerType.ProjectTask => await MoveTask(request.ItemId, request.TargetParentId, newOrderKey, ct),
            _ => Result.Failure(Error.Validation("Item.UnknownType", $"Unknown item type: {request.ItemType}"))
        };

        if (result.IsFailure) return result;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<string> ResolveOrderKey(MoveItemCommand request, CancellationToken ct)
    {
        if (request.PreviousItemOrderKey != null && request.NextItemOrderKey != null)
        {
            if (string.Compare(request.PreviousItemOrderKey, request.NextItemOrderKey, StringComparison.Ordinal) >= 0)
                return FractionalIndex.After(request.PreviousItemOrderKey);

            return FractionalIndex.Between(request.PreviousItemOrderKey, request.NextItemOrderKey);
        }

        if (request.PreviousItemOrderKey != null) return FractionalIndex.After(request.PreviousItemOrderKey);
        if (request.NextItemOrderKey != null) return FractionalIndex.Before(request.NextItemOrderKey);

        var maxKey = await GetMaxOrderKey(request, ct);
        return maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);
    }

    private async Task<string?> GetMaxOrderKey(MoveItemCommand request, CancellationToken ct)
    {
        var workspaceId = context.workspaceId;

        return request.ItemType switch
        {
            EntityLayerType.ProjectSpace => await db.Spaces
                                .ByWorkspace(workspaceId)
                                .WhereNotDeleted()
                                .MaxAsync(s => (string?)s.OrderKey, ct),

            EntityLayerType.ProjectFolder => await db.Folders
                                .BySpace(request.TargetParentId.GetValueOrDefault())
                                .WhereNotDeleted()
                                .MaxAsync(f => (string?)f.OrderKey, ct),

            EntityLayerType.ProjectTask => await db.Tasks
                                .Where(t => (request.TargetParentId.HasValue && t.ProjectFolderId == request.TargetParentId) ||
                                           (!request.TargetParentId.HasValue && t.ProjectSpaceId == workspaceId && t.ProjectFolderId == null))
                                .WhereNotDeleted()
                                .MaxAsync(t => (string?)t.OrderKey, ct),

            _ => null
        };
    }

    private async Task<Result> MoveSpace(Guid spaceId, string newOrderKey, CancellationToken ct)
    {
        var space = await db.Spaces.FindAsync([spaceId], ct);
        if (space == null) return Result.Failure(SpaceError.NotFound);

        space.UpdateOrderKey(newOrderKey);
        return Result.Success();
    }

    private async Task<Result> MoveFolder(Guid folderId, Guid? newSpaceId, string newOrderKey, CancellationToken ct)
    {
        var folder = await db.Folders.FindAsync([folderId], ct);
        if (folder == null) return Result.Failure(Error.NotFound("Folder.NotFound", $"Folder {folderId} not found"));

        if (newSpaceId.HasValue && newSpaceId.Value != folder.ProjectSpaceId)
        {
            var newSpaceExists = await db.Spaces.AnyAsync(s => s.Id == newSpaceId.Value, ct);
            if (!newSpaceExists) return Result.Failure(SpaceError.NotFound);

            await db.Folders
                .Where(f => f.Id == folderId)
                .ExecuteUpdateAsync(updates =>
                    updates.SetProperty(f => f.ProjectSpaceId, newSpaceId.Value)
                           .SetProperty(f => f.OrderKey, newOrderKey)
                           .SetProperty(f => f.UpdatedAt, DateTimeOffset.UtcNow),
                    cancellationToken: ct);
        }
        else
        {
            folder.UpdateOrderKey(newOrderKey);
        }
        return Result.Success();
    }

    private async Task<Result> MoveTask(Guid taskId, Guid? targetParentId, string newOrderKey, CancellationToken ct)
    {
        var task = await db.Tasks.FindAsync([taskId], ct);
        if (task == null) return Result.Failure(Error.NotFound("Task.NotFound", $"Task {taskId} not found"));

        Guid? resolvedSpaceId = null;
        Guid? resolvedFolderId = null;

        if (targetParentId.HasValue)
        {
            var folder = await db.Folders.FindAsync([targetParentId.Value], ct);
            if (folder != null)
            {
                resolvedFolderId = folder.Id;
                resolvedSpaceId = folder.ProjectSpaceId;
            }
            else
            {
                var space = await db.Spaces.FindAsync([targetParentId.Value], ct);
                if (space != null)
                {
                    resolvedSpaceId = space.Id;
                    resolvedFolderId = null;
                }
            }
        }

        if (resolvedSpaceId == null) return Result.Failure(Error.Validation("MoveTask.InvalidTarget", "Target parent must be a valid folder or space."));

        await db.Tasks
            .Where(t => t.Id == taskId)
            .ExecuteUpdateAsync(updates =>
                updates.SetProperty(t => t.ProjectSpaceId, resolvedSpaceId)
                       .SetProperty(t => t.ProjectFolderId, resolvedFolderId)
                       .SetProperty(t => t.OrderKey, newOrderKey)
                       .SetProperty(t => t.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken: ct);

        return Result.Success();
    }
}
