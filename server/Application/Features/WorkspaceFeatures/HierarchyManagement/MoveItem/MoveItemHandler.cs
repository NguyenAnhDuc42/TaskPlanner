using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.MoveItem;

public class MoveItemHandler : BaseCommandHandler, IRequestHandler<MoveItemCommand, Unit>
{
    public MoveItemHandler(
        IUnitOfWork unitOfWork,
        IPermissionService permissionService,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext)
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
        var space = await UnitOfWork.Set<ProjectSpace>()
            .Where(s => s.Id == spaceId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Space {spaceId} not found");

        // Permission check
        await RequirePermissionAsync(space, PermissionAction.Edit, cancellationToken);

        // Update order key
        space.Update(orderKey: newOrderKey);
    }

    private async Task MoveFolder(Guid folderId, Guid? newSpaceId, long newOrderKey, CancellationToken cancellationToken)
    {
        var folder = await UnitOfWork.Set<ProjectFolder>()
            .Where(f => f.Id == folderId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Folder {folderId} not found");

        // Permission check
        await RequirePermissionAsync(folder, PermissionAction.Edit, cancellationToken);

        // If moving to a different space, validate it exists
        if (newSpaceId.HasValue && newSpaceId.Value != folder.ProjectSpaceId)
        {
            var newSpace = await UnitOfWork.Set<ProjectSpace>()
                .Where(s => s.Id == newSpaceId.Value)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new KeyNotFoundException($"Target space {newSpaceId.Value} not found");

            // Permission check on target space
            await RequirePermissionAsync(newSpace, PermissionAction.Edit, cancellationToken);

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
        var list = await UnitOfWork.Set<ProjectList>()
            .Where(l => l.Id == listId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"List {listId} not found");

        // Permission check
        await RequirePermissionAsync(list, PermissionAction.Edit, cancellationToken);

        // Determine if moving to a folder or directly under a space
        if (newParentId.HasValue)
        {
            // Try to find as folder first
            var targetFolder = await UnitOfWork.Set<ProjectFolder>()
                .Where(f => f.Id == newParentId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (targetFolder != null)
            {
                // Moving to a folder
                await RequirePermissionAsync(targetFolder, PermissionAction.Edit, cancellationToken);

                list.Update(orderKey: newOrderKey, projectFolderId: newParentId.Value);
            }
            else
            {
                // Try to find as space
                var targetSpace = await UnitOfWork.Set<ProjectSpace>()
                    .Where(s => s.Id == newParentId.Value)
                    .FirstOrDefaultAsync(cancellationToken)
                    ?? throw new KeyNotFoundException($"Target parent {newParentId.Value} not found");

                // Moving directly under a space (no folder)
                await RequirePermissionAsync(targetSpace, PermissionAction.Edit, cancellationToken);

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
