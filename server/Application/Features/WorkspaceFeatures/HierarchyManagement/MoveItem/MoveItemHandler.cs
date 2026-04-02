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
    private const long DefaultIncrement = 10_000_000L;

    public MoveItemHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext)
    {
    }

    public async Task<Unit> Handle(MoveItemCommand request, CancellationToken cancellationToken)
    {
        var newOrderKey = await ResolveOrderKey(request, cancellationToken);

        switch (request.ItemType)
        {
            case ItemType.Space:
                await MoveSpace(request.ItemId, newOrderKey, cancellationToken);
                break;
            case ItemType.Folder:
                await MoveFolder(request.ItemId, request.TargetParentId, newOrderKey, cancellationToken);
                break;
            case ItemType.Task:
                await MoveTask(request.ItemId, request.TargetParentId, newOrderKey, cancellationToken);
                break;
            default:
                throw new ArgumentException($"Unknown item type: {request.ItemType}");
        }

        return Unit.Value;
    }

    private async Task<long> ResolveOrderKey(MoveItemCommand request, CancellationToken cancellationToken)
    {
        // Case 1: Simple fractional indexing
        if (request.PreviousItemOrderKey.HasValue || request.NextItemOrderKey.HasValue)
        {
            var key = CalculateFractionalKey(request.PreviousItemOrderKey, request.NextItemOrderKey);
            
            // Safety check: if keys are too close (collision), fall back to max + buffer
            if (request.PreviousItemOrderKey.HasValue && request.NextItemOrderKey.HasValue && 
                Math.Abs(request.NextItemOrderKey.Value - request.PreviousItemOrderKey.Value) <= 1)
            {
                return await GetMaxOrderKey(request, cancellationToken) + DefaultIncrement;
            }
            
            return key;
        }

        // Case 2: No context provided (e.g. initial add or "bug out" fallback)
        // Pick the latest number based on the upper layer as requested
        return await GetMaxOrderKey(request, cancellationToken) + DefaultIncrement;
    }

    private async Task<long> GetMaxOrderKey(MoveItemCommand request, CancellationToken cancellationToken)
    {
        IQueryable<long?> query = request.ItemType switch
        {
            ItemType.Space => UnitOfWork.Set<ProjectSpace>()
                                .Where(s => s.ProjectWorkspaceId == WorkspaceId)
                                .Select(s => (long?)s.OrderKey),
            ItemType.Folder => UnitOfWork.Set<ProjectFolder>()
                                .Where(f => f.ProjectSpaceId == request.TargetParentId)
                                .Select(f => (long?)f.OrderKey),
            ItemType.Task => UnitOfWork.Set<ProjectTask>()
                                .Where(t => (request.TargetParentId.HasValue && t.ProjectFolderId == request.TargetParentId) || 
                                           (!request.TargetParentId.HasValue && t.ProjectSpaceId == WorkspaceId)) // Workspace as direct parent fallback
                                .Select(t => t.OrderKey),
            _ => throw new ArgumentOutOfRangeException()
        };

        return await query.MaxAsync(cancellationToken) ?? 0L;
    }

    private long CalculateFractionalKey(long? previous, long? next)
    {
        if (!previous.HasValue && next.HasValue) return next.Value / 2;
        if (previous.HasValue && !next.HasValue) return previous.Value + DefaultIncrement;
        if (previous.HasValue && next.HasValue) return (previous.Value + next.Value) / 2;
        return DefaultIncrement;
    }

    private async Task MoveSpace(Guid spaceId, long newOrderKey, CancellationToken cancellationToken)
    {
        var space = await UnitOfWork.Set<ProjectSpace>().FindAsync(spaceId, cancellationToken);
        if (space == null) throw new KeyNotFoundException($"Space {spaceId} not found");

        space.UpdateOrderKey(newOrderKey);
    }

    private async Task MoveFolder(Guid folderId, Guid? newSpaceId, long newOrderKey, CancellationToken cancellationToken)
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

    private async Task MoveTask(Guid taskId, Guid? targetParentId, long newOrderKey, CancellationToken cancellationToken)
    {
        var task = await UnitOfWork.Set<ProjectTask>().FindAsync(taskId, cancellationToken);
        if (task == null) throw new KeyNotFoundException($"Task {taskId} not found");

        Guid? resolvedSpaceId = null;
        Guid? resolvedFolderId = null;

        if (targetParentId.HasValue)
        {
            // Resolve parent (could be folder or space)
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
