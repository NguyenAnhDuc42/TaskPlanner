using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features;
using Application.Interfaces;
using Application.Interfaces.Data;
using Domain.Common;
using Domain.Entities;
using Domain.Entities.ProjectEntities;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.MoveItem;

public class MoveItemHandler : ICommandHandler<MoveItemCommand>
{
    private readonly IDataBase _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly WorkspaceContext _workspaceContext;

    public MoveItemHandler(IDataBase db, ICurrentUserService currentUserService, WorkspaceContext workspaceContext) {
        _db = db;
        _currentUserService = currentUserService;
        _workspaceContext = workspaceContext;
    }

    public async Task<Result> Handle(MoveItemCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) return Result.Failure(Error.Unauthorized("User.NotAuthenticated", "User not authenticated."));

        var newOrderKey = request.NewOrderKey ?? await ResolveOrderKey(request, cancellationToken);

        switch (request.ItemType)
        {
            case Domain.Enums.RelationShip.EntityLayerType.ProjectSpace:
                var spaceResult = await MoveSpace(request.ItemId, newOrderKey, cancellationToken);
                if (spaceResult.IsFailure) return spaceResult;
                break;
            case Domain.Enums.RelationShip.EntityLayerType.ProjectFolder:
                var folderResult = await MoveFolder(request.ItemId, request.TargetParentId, newOrderKey, cancellationToken);
                if (folderResult.IsFailure) return folderResult;
                break;
            case Domain.Enums.RelationShip.EntityLayerType.ProjectTask:
                var taskResult = await MoveTask(request.ItemId, request.TargetParentId, newOrderKey, cancellationToken);
                if (taskResult.IsFailure) return taskResult;
                break;
            default:
                return Result.Failure(Error.Validation("Item.UnknownType", $"Unknown item type: {request.ItemType}"));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<string> ResolveOrderKey(MoveItemCommand request, CancellationToken cancellationToken)
    {
        // Case 1: Both neighbours known — compute midpoint
        if (request.PreviousItemOrderKey != null && request.NextItemOrderKey != null)
        {
            // Safety: If keys are equal or inverted, fallback to simple After() to avoid crash
            if (string.Compare(request.PreviousItemOrderKey, request.NextItemOrderKey, StringComparison.Ordinal) >= 0)
            {
                return FractionalIndex.After(request.PreviousItemOrderKey);
            }
            return FractionalIndex.Between(request.PreviousItemOrderKey, request.NextItemOrderKey);
        }

        // Case 2: Only one neighbour known
        if (request.PreviousItemOrderKey != null) return FractionalIndex.After(request.PreviousItemOrderKey);
        if (request.NextItemOrderKey != null) return FractionalIndex.Before(request.NextItemOrderKey);

        // Case 3: No context — append at the end
        var maxKey = await GetMaxOrderKey(request, cancellationToken);
        return maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);
    }

    private async Task<string?> GetMaxOrderKey(MoveItemCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = _workspaceContext.workspaceId;
        
        return request.ItemType switch
        {
            Domain.Enums.RelationShip.EntityLayerType.ProjectSpace => await _db.Spaces
                                .ByWorkspace(workspaceId)
                                .WhereNotDeleted()
                                .MaxAsync(s => (string?)s.OrderKey, cancellationToken),

            Domain.Enums.RelationShip.EntityLayerType.ProjectFolder => await _db.Folders
                                .BySpace(request.TargetParentId.GetValueOrDefault())
                                .WhereNotDeleted()
                                .MaxAsync(f => (string?)f.OrderKey, cancellationToken),

            Domain.Enums.RelationShip.EntityLayerType.ProjectTask => await _db.Tasks
                                .Where(t => (request.TargetParentId.HasValue && t.ProjectFolderId == request.TargetParentId) ||
                                           (!request.TargetParentId.HasValue && t.ProjectSpaceId == workspaceId && t.ProjectFolderId == null))
                                .WhereNotDeleted()
                                .MaxAsync(t => (string?)t.OrderKey, cancellationToken),

            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private async Task<Result> MoveSpace(Guid spaceId, string newOrderKey, CancellationToken cancellationToken)
    {
        var space = await _db.Spaces.FindAsync(new object[] { spaceId }, cancellationToken);
        if (space == null) return Result.Failure(Error.NotFound("Space.NotFound", $"Space {spaceId} not found"));
        space.UpdateOrderKey(newOrderKey);
        return Result.Success();
    }

    private async Task<Result> MoveFolder(Guid folderId, Guid? newSpaceId, string newOrderKey, CancellationToken cancellationToken)
    {
        var folder = await _db.Folders.FindAsync(new object[] { folderId }, cancellationToken);
        if (folder == null) return Result.Failure(Error.NotFound("Folder.NotFound", $"Folder {folderId} not found"));

        if (newSpaceId.HasValue && newSpaceId.Value != folder.ProjectSpaceId)
        {
            var newSpace = await _db.Spaces.FindAsync(new object[] { newSpaceId.Value }, cancellationToken);
            if (newSpace == null) return Result.Failure(Error.NotFound("Space.NotFound", $"Space {newSpaceId.Value} not found"));

            await _db.Folders
                .Where(f => f.Id == folderId)
                .ExecuteUpdateAsync(updates =>
                    updates.SetProperty(f => f.ProjectSpaceId, newSpaceId.Value)
                           .SetProperty(f => f.OrderKey, newOrderKey)
                           .SetProperty(f => f.UpdatedAt, DateTimeOffset.UtcNow),
                    cancellationToken: cancellationToken);
        }
        else
        {
            folder.UpdateOrderKey(newOrderKey);
        }
        return Result.Success();
    }

    private async Task<Result> MoveTask(Guid taskId, Guid? targetParentId, string newOrderKey, CancellationToken cancellationToken)
    {
        var task = await _db.Tasks.FindAsync(new object[] { taskId }, cancellationToken);
        if (task == null) return Result.Failure(Error.NotFound("Task.NotFound", $"Task {taskId} not found"));

        Guid? resolvedSpaceId = null;
        Guid? resolvedFolderId = null;

        if (targetParentId.HasValue)
        {
            var folder = await _db.Folders.FindAsync(new object[] { targetParentId.Value }, cancellationToken);
            if (folder != null)
            {
                resolvedFolderId = folder.Id;
                resolvedSpaceId = folder.ProjectSpaceId;
            }
            else
            {
                var space = await _db.Spaces.FindAsync(new object[] { targetParentId.Value }, cancellationToken);
                if (space != null)
                {
                    resolvedSpaceId = space.Id;
                    resolvedFolderId = null;
                }
            }
        }

        if (resolvedSpaceId == null) return Result.Failure(Error.Validation("MoveTask.InvalidTarget", "Target parent must be a valid folder or space."));
        
        await _db.Tasks
            .Where(t => t.Id == taskId)
            .ExecuteUpdateAsync(updates =>
                updates.SetProperty(t => t.ProjectSpaceId, resolvedSpaceId)
                       .SetProperty(t => t.ProjectFolderId, resolvedFolderId)
                       .SetProperty(t => t.OrderKey, newOrderKey)
                       .SetProperty(t => t.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);
                
        return Result.Success();
    }
}
