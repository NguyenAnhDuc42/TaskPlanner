using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
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
        var newOrderKey = CalculateNewOrderKey(request.PreviousItemOrderKey, request.NextItemOrderKey);

        switch (request.ItemType)
        {
            case ItemType.Space:
                await MoveSpace(request.ItemId, newOrderKey, cancellationToken);
                break;
            case ItemType.Folder:
                await MoveFolder(request.ItemId, request.TargetParentId, newOrderKey, cancellationToken);
                break;
            case ItemType.List:
                await MoveList(request.ItemId, request.TargetParentId, newOrderKey, cancellationToken);
                break;
            default:
                throw new ArgumentException($"Unknown item type: {request.ItemType}");
        }

        return Unit.Value;
    }

    private async Task MoveSpace(Guid spaceId, long newOrderKey, CancellationToken cancellationToken)
    {
        var space = await FindOrThrowAsync<ProjectSpace>(spaceId);

        // Update order key
        space.Update(orderKey: newOrderKey);
    }

    private async Task MoveFolder(Guid folderId, Guid? newSpaceId, long newOrderKey, CancellationToken cancellationToken)
    {
        var folder = await FindOrThrowAsync<ProjectFolder>(folderId);

        // If moving to a different space, validate it exists and we have permissions
        if (newSpaceId.HasValue && newSpaceId.Value != folder.ProjectSpaceId)
        {
            var newSpace = await FindOrThrowAsync<ProjectSpace>(newSpaceId.Value);

            // Update folder's space and order key
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
            // Just reorder within same space
            folder.Update(orderKey: newOrderKey);
        }
    }

    private async Task MoveList(Guid listId, Guid? newParentId, long newOrderKey, CancellationToken cancellationToken)
    {
        var list = await FindOrThrowAsync<ProjectList>(listId);

        // Determine if moving to a folder or directly under a space
        if (newParentId.HasValue)
        {
            // Try to find as folder first
            var targetFolder = await UnitOfWork.Set<ProjectFolder>()
                .Where(f => f.Id == newParentId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (targetFolder != null)
            {
                // Moving to a folder - Authorize on target folder
                await FindOrThrowAsync<ProjectFolder>(targetFolder.Id);

                list.Update(orderKey: newOrderKey, projectFolderId: newParentId.Value);
            }
            else
            {
                // Try to find as space - Authorize on target space
                var targetSpace = await FindOrThrowAsync<ProjectSpace>(newParentId.Value);

                // Moving directly under a space (no folder)
                await UnitOfWork.Set<ProjectList>()
                    .Where(l => l.Id == listId)
                    .ExecuteUpdateAsync(updates =>
                        updates.SetProperty(l => l.ProjectSpaceId, targetSpace.Id)
                               .SetProperty(l => l.ProjectFolderId, (Guid?)null)
                               .SetProperty(l => l.OrderKey, newOrderKey)
                               .SetProperty(l => l.UpdatedAt, DateTimeOffset.UtcNow),
                        cancellationToken: cancellationToken);
            }
        }
        else
        {
            // Just reorder within same parent
            list.Update(orderKey: newOrderKey);
        }
    }

    /// <summary>
    /// Calculate new order key using fractional indexing
    /// </summary>
    private long CalculateNewOrderKey(long? previousOrderKey, long? nextOrderKey)
    {
        // Moving to top (no previous item)
        if (!previousOrderKey.HasValue && nextOrderKey.HasValue)
        {
            return nextOrderKey.Value / 2;
        }

        // Moving to bottom (no next item)
        if (previousOrderKey.HasValue && !nextOrderKey.HasValue)
        {
            return previousOrderKey.Value + 10_000_000L;
        }

        // Moving between two items
        if (previousOrderKey.HasValue && nextOrderKey.HasValue)
        {
            return (previousOrderKey.Value + nextOrderKey.Value) / 2;
        }

        // Fallback (shouldn't happen)
        return 10_000_000L;
    }
}
