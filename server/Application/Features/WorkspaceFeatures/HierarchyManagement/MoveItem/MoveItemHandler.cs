using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Common;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.MoveItem;

public class MoveItemHandler : BaseFeatureHandler, IRequestHandler<MoveItemCommand, Unit>
{
    public MoveItemHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext)
    {
    }

    public async Task<Unit> Handle(MoveItemCommand request, CancellationToken cancellationToken)
    {
        var newOrderKey = request.NewOrderKey ?? await ResolveOrderKey(request, cancellationToken);

        switch (request.ItemType)
        {
            case Domain.Enums.RelationShip.EntityLayerType.ProjectSpace:
                await MoveSpace(request.ItemId, newOrderKey, cancellationToken);
                break;
            case Domain.Enums.RelationShip.EntityLayerType.ProjectFolder:
                await MoveFolder(request.ItemId, request.TargetParentId, newOrderKey, cancellationToken);
                break;
            case Domain.Enums.RelationShip.EntityLayerType.ProjectTask:
                await MoveTask(request.ItemId, request.TargetParentId, newOrderKey, cancellationToken);
                break;
            default:
                throw new ArgumentException($"Unknown item type: {request.ItemType}");
        }

        return Unit.Value;
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
        return request.ItemType switch
        {
            Domain.Enums.RelationShip.EntityLayerType.ProjectSpace => await UnitOfWork.Set<ProjectSpace>()
                                .Where(s => s.ProjectWorkspaceId == WorkspaceId && s.DeletedAt == null)
                                .MaxAsync(s => (string?)s.OrderKey, cancellationToken),

            Domain.Enums.RelationShip.EntityLayerType.ProjectFolder => await UnitOfWork.Set<ProjectFolder>()
                                .Where(f => f.ProjectSpaceId == request.TargetParentId && f.DeletedAt == null)
                                .MaxAsync(f => (string?)f.OrderKey, cancellationToken),

            Domain.Enums.RelationShip.EntityLayerType.ProjectTask => await UnitOfWork.Set<ProjectTask>()
                                .Where(t => (request.TargetParentId.HasValue && t.ProjectFolderId == request.TargetParentId) ||
                                           (!request.TargetParentId.HasValue && t.ProjectSpaceId == WorkspaceId && t.ProjectFolderId == null))
                                .MaxAsync(t => (string?)t.OrderKey, cancellationToken),

            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private async Task MoveSpace(Guid spaceId, string newOrderKey, CancellationToken cancellationToken)
    {
        var space = await UnitOfWork.Set<ProjectSpace>().FindAsync(spaceId, cancellationToken);
        if (space == null) throw new KeyNotFoundException($"Space {spaceId} not found");
        space.UpdateOrderKey(newOrderKey);
    }

    private async Task MoveFolder(Guid folderId, Guid? newSpaceId, string newOrderKey, CancellationToken cancellationToken)
    {
        var folder = await UnitOfWork.Set<ProjectFolder>().FindAsync(folderId, cancellationToken);
        if (folder == null) throw new KeyNotFoundException($"Folder {folderId} not found");

        if (newSpaceId.HasValue && newSpaceId.Value != folder.ProjectSpaceId)
        {
            var newSpace = await UnitOfWork.Set<ProjectSpace>().FindAsync(newSpaceId.Value, cancellationToken);
            if (newSpace == null) throw new KeyNotFoundException($"Space {newSpaceId.Value} not found");

            await UnitOfWork.Set<ProjectFolder>()
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
    }

    private async Task MoveTask(Guid taskId, Guid? targetParentId, string newOrderKey, CancellationToken cancellationToken)
    {
        var task = await UnitOfWork.Set<ProjectTask>().FindAsync(taskId, cancellationToken);
        if (task == null) throw new KeyNotFoundException($"Task {taskId} not found");

        Guid? resolvedSpaceId = null;
        Guid? resolvedFolderId = null;

        if (targetParentId.HasValue)
        {
            var folder = await UnitOfWork.Set<ProjectFolder>().FindAsync(targetParentId.Value, cancellationToken);
            if (folder != null)
            {
                resolvedFolderId = folder.Id;
                resolvedSpaceId = folder.ProjectSpaceId;
            }
            else
            {
                var space = await UnitOfWork.Set<ProjectSpace>().FindAsync(targetParentId.Value, cancellationToken);
                if (space != null)
                {
                    resolvedSpaceId = space.Id;
                    resolvedFolderId = null;
                }
            }
        }

        if (resolvedSpaceId == null) throw new ArgumentException("Target parent must be a valid folder or space.");
        await UnitOfWork.Set<ProjectTask>()
            .Where(t => t.Id == taskId)
            .ExecuteUpdateAsync(updates =>
                updates.SetProperty(t => t.ProjectSpaceId, resolvedSpaceId)
                       .SetProperty(t => t.ProjectFolderId, resolvedFolderId)
                       .SetProperty(t => t.OrderKey, newOrderKey)
                       .SetProperty(t => t.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);
    }
}
