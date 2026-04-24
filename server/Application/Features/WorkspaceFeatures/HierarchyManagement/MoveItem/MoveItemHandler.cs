using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Common;
using Domain.Entities;
using Domain.Enums.RelationShip;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.WorkspaceFeatures;

public class MoveItemHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<MoveItemCommand>
{
    public async Task<Result> Handle(MoveItemCommand request, CancellationToken ct)
    {
        // 1. Permission: Only Admin/Owner can reorganize hierarchy across workspace
        if (context.CurrentMember.Role > Role.Admin) 
            return Result.Failure(MemberError.DontHavePermission);

        // 2. Resolve New OrderKey (O(1) if provided by FE, else O(log N) with index)
        var newOrderKey = request.NewOrderKey ?? await ResolveOrderKey(request, ct);

        // 3. PERFORMANCE: Branch to optimized direct updates
        return request.ItemType switch
        {
            EntityLayerType.ProjectSpace => await MoveSpace(request.ItemId, newOrderKey, ct),
            EntityLayerType.ProjectFolder => await MoveFolder(request.ItemId, request.TargetParentId, newOrderKey, ct),
            EntityLayerType.ProjectTask => await MoveTask(request.ItemId, request.TargetParentId, newOrderKey, ct),
            _ => Result.Failure(Error.Validation("Item.UnknownType", $"Unknown item type: {request.ItemType}"))
        };
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
        // PERFORMANCE: These queries MUST be supported by composite indexes: (ParentId, OrderKey)
        return request.ItemType switch
        {
            EntityLayerType.ProjectSpace => await db.Spaces
                                .AsNoTracking()
                                .ByWorkspace(context.workspaceId)
                                .MaxAsync(s => (string?)s.OrderKey, ct),

            EntityLayerType.ProjectFolder => await db.Folders
                                .AsNoTracking()
                                .BySpace(request.TargetParentId.GetValueOrDefault())
                                .MaxAsync(f => (string?)f.OrderKey, ct),

            EntityLayerType.ProjectTask => await db.Tasks
                                .AsNoTracking()
                                .Where(t => (request.TargetParentId.HasValue && t.ProjectFolderId == request.TargetParentId) ||
                                           (!request.TargetParentId.HasValue && t.ProjectSpaceId == request.ItemId && t.ProjectFolderId == null)) // ItemId is parent for Space moves
                                .MaxAsync(t => (string?)t.OrderKey, ct),

            _ => null
        };
    }

    private async Task<Result> MoveSpace(Guid spaceId, string newOrderKey, CancellationToken ct)
    {
        // PERFORMANCE: Single SQL UPDATE, zero entity allocations
        var affected = await db.Spaces
            .Where(s => s.Id == spaceId && s.ProjectWorkspaceId == context.workspaceId)
            .ExecuteUpdateAsync(u => u.SetProperty(s => s.OrderKey, newOrderKey)
                                      .SetProperty(s => s.UpdatedAt, DateTimeOffset.UtcNow), ct);

        return affected > 0 ? Result.Success() : Result.Failure(SpaceError.NotFound);
    }

    private async Task<Result> MoveFolder(Guid folderId, Guid? newSpaceId, string newOrderKey, CancellationToken ct)
    {
        // PERFORMANCE: Direct UPDATE with conditional parent change
        var affected = await db.Folders
            .Where(f => f.Id == folderId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(f => f.ProjectSpaceId, f => newSpaceId ?? f.ProjectSpaceId)
                .SetProperty(f => f.OrderKey, newOrderKey)
                .SetProperty(f => f.UpdatedAt, DateTimeOffset.UtcNow), ct);

        return affected > 0 ? Result.Success() : Result.Failure(FolderError.NotFound);
    }

    private async Task<Result> MoveTask(Guid taskId, Guid? targetParentId, string newOrderKey, CancellationToken ct)
    {
        // 1. Resolve Layer Context: We need to know if TargetParent is a Space or Folder
        // PERFORMANCE: Small allocation but necessary for Task cross-layer movements
        Guid? resolvedSpaceId = null;
        Guid? resolvedFolderId = null;

        if (targetParentId.HasValue)
        {
            var targetInfo = await db.Spaces
                .Where(s => s.Id == targetParentId.Value)
                .Select(s => new { Id = s.Id, Type = "ProjectSpace" })
                .Union(db.Folders.Where(f => f.Id == targetParentId.Value).Select(f => new { Id = f.ProjectSpaceId, Type = "ProjectFolder" }))
                .FirstOrDefaultAsync(ct);

            if (targetInfo == null) return Result.Failure(Error.Validation("MoveTask.InvalidTarget", "Target parent not found."));
            
            if (targetInfo.Type == "ProjectSpace") resolvedSpaceId = targetParentId;
            else { resolvedFolderId = targetParentId; resolvedSpaceId = targetInfo.Id; }
        }

        if (resolvedSpaceId == null) return Result.Failure(Error.Validation("MoveTask.InvalidTarget", "No valid target space resolved."));

        // 2. PERFORMANCE: Direct Atomic Update
        var affected = await db.Tasks
            .Where(t => t.Id == taskId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(t => t.ProjectSpaceId, resolvedSpaceId)
                .SetProperty(t => t.ProjectFolderId, resolvedFolderId)
                .SetProperty(t => t.OrderKey, newOrderKey)
                .SetProperty(t => t.UpdatedAt, DateTimeOffset.UtcNow), ct);

        return affected > 0 ? Result.Success() : Result.Failure(Error.NotFound("Task.NotFound", "Task not found"));
    }
}
