using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.EntityAccessManagement.GetEntityAccessList;

public class GetEntityAccessListHandler : BaseFeatureHandler, IRequestHandler<GetEntityAccessListQuery, List<EntityAccessMemberDto>>
{
    public GetEntityAccessListHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<List<EntityAccessMemberDto>> Handle(GetEntityAccessListQuery request, CancellationToken cancellationToken)
    {
        var (resolvedId, resolvedType) = await GetEffectiveAccessLayer(request.LayerId, request.LayerType);
        var isInheritedFromParent = resolvedId != request.LayerId || resolvedType != request.LayerType;

        // Resolve creator ID for the layer for "Creator" badge
        Guid? creatorId = request.LayerType switch
        {
            EntityLayerType.ProjectWorkspace => await UnitOfWork.Set<ProjectWorkspace>().Where(x => x.Id == request.LayerId).Select(x => x.CreatorId).FirstOrDefaultAsync(cancellationToken),
            EntityLayerType.ProjectSpace => await UnitOfWork.Set<ProjectSpace>().Where(x => x.Id == request.LayerId).Select(x => x.CreatorId).FirstOrDefaultAsync(cancellationToken),
            EntityLayerType.ProjectFolder => await UnitOfWork.Set<ProjectFolder>().Where(x => x.Id == request.LayerId).Select(x => x.CreatorId).FirstOrDefaultAsync(cancellationToken),
            EntityLayerType.ChatRoom => await UnitOfWork.Set<ChatRoom>().Where(x => x.Id == request.LayerId).Select(x => x.CreatorId).FirstOrDefaultAsync(cancellationToken),
            _ => null
        };

        var explicitAccess = await UnitOfWork.Set<EntityAccess>()
            .AsNoTracking()
            .Where(ea => ea.ProjectWorkspaceId == WorkspaceId 
                      && ea.EntityId == resolvedId 
                      && ea.EntityLayer == resolvedType 
                      && ea.DeletedAt == null)
            .ToDictionaryAsync(ea => ea.WorkspaceMemberId, cancellationToken);

        if (request.IsManagementMode)
        {
            var parentScopedMemberIds = await GetParentScopedMemberIds(request.LayerId, request.LayerType, cancellationToken);
            return await GetManagementModeMembers(explicitAccess, resolvedType, creatorId, isInheritedFromParent, parentScopedMemberIds, cancellationToken);
        }

        return await GetAssignableModeMembers(explicitAccess, resolvedType, creatorId, isInheritedFromParent, cancellationToken);
    }

    private async Task<List<EntityAccessMemberDto>> GetManagementModeMembers(
        Dictionary<Guid, EntityAccess> explicitAccess,
        EntityLayerType resolvedType,
        Guid? creatorId,
        bool isInheritedFromParent,
        List<Guid>? parentScopedMemberIds,
        CancellationToken cancellationToken)
    {
        var query = UnitOfWork.Set<WorkspaceMember>()
            .Include(wm => wm.User)
            .AsNoTracking()
            .Where(wm => wm.ProjectWorkspaceId == WorkspaceId && wm.DeletedAt == null);

        // Management candidates must come from the effective parent chain.
        // If parent chain resolves to Workspace, parentScopedMemberIds is null and all workspace members are allowed.
        if (parentScopedMemberIds is not null)
        {
            query = query.Where(wm => parentScopedMemberIds.Contains(wm.Id));
        }

        var members = await query.ToListAsync(cancellationToken);

        return members.Select(wm =>
        {
            var explicitLevel = explicitAccess.TryGetValue(wm.Id, out var ea) ? ea.AccessLevel : (AccessLevel?)null;
            var effectiveLevel = explicitLevel ?? (resolvedType == EntityLayerType.ProjectWorkspace ? AccessLevel.Editor : AccessLevel.None);
            var isInherited = isInheritedFromParent || effectiveLevel != explicitLevel;

            return new EntityAccessMemberDto(
                wm.Id,
                wm.UserId,
                wm.User.Name,
                wm.User.Email,
                wm.Role,
                explicitLevel,
                effectiveLevel,
                isInherited,
                wm.CreatedAt,
                creatorId.HasValue && creatorId.Value == wm.UserId,
                wm.UserId == CurrentUserId
            );
        }).ToList();
    }

    private async Task<List<EntityAccessMemberDto>> GetAssignableModeMembers(
        Dictionary<Guid, EntityAccess> explicitAccess,
        EntityLayerType resolvedType,
        Guid? creatorId,
        bool isInheritedFromParent,
        CancellationToken cancellationToken)
    {
        var query = UnitOfWork.Set<WorkspaceMember>()
            .Include(wm => wm.User)
            .AsNoTracking()
            .Where(wm => wm.ProjectWorkspaceId == WorkspaceId && wm.DeletedAt == null);

        if (resolvedType != EntityLayerType.ProjectWorkspace)
        {
            var activeMemberIds = explicitAccess.Keys.ToList();
            query = query.Where(wm => activeMemberIds.Contains(wm.Id));
        }

        var results = await query.ToListAsync(cancellationToken);

        return results.Select(wm =>
        {
            var explicitLevel = explicitAccess.TryGetValue(wm.Id, out var ea) ? ea.AccessLevel : (AccessLevel?)null;
            var effectiveLevel = explicitLevel ?? (resolvedType == EntityLayerType.ProjectWorkspace ? AccessLevel.Editor : AccessLevel.None);
            var isInherited = isInheritedFromParent || effectiveLevel != explicitLevel;

            return new EntityAccessMemberDto(
                wm.Id,
                wm.UserId,
                wm.User.Name,
                wm.User.Email,
                wm.Role,
                explicitLevel,
                effectiveLevel,
                isInherited,
                wm.CreatedAt,
                creatorId.HasValue && creatorId.Value == wm.UserId,
                wm.UserId == CurrentUserId
            );
        }).ToList();
    }

    private async Task<List<Guid>?> GetParentScopedMemberIds(
        Guid layerId,
        EntityLayerType layerType,
        CancellationToken cancellationToken)
    {
        if (layerType == EntityLayerType.ProjectWorkspace)
            return null;

        var parent = await GetParentLayer(layerId, layerType, cancellationToken);
        var (parentResolvedId, parentResolvedType) = await GetEffectiveAccessLayer(parent.Id, parent.Type);

        if (parentResolvedType == EntityLayerType.ProjectWorkspace)
            return null;

        return await UnitOfWork.Set<EntityAccess>()
            .AsNoTracking()
            .Where(ea =>
                ea.ProjectWorkspaceId == WorkspaceId &&
                ea.EntityId == parentResolvedId &&
                ea.EntityLayer == parentResolvedType &&
                ea.DeletedAt == null)
            .Select(ea => ea.WorkspaceMemberId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private async Task<(Guid Id, EntityLayerType Type)> GetParentLayer(
        Guid layerId,
        EntityLayerType layerType,
        CancellationToken cancellationToken)
    {
        return layerType switch
        {
            EntityLayerType.ProjectSpace => (WorkspaceId, EntityLayerType.ProjectWorkspace),

            EntityLayerType.ProjectFolder => await ResolveFolderParent(layerId, cancellationToken),

            _ => throw new ArgumentOutOfRangeException(nameof(layerType), layerType, "Unsupported layer type for parent resolution")
        };
    }



    private async Task<(Guid Id, EntityLayerType Type)> ResolveFolderParent(Guid folderId, CancellationToken cancellationToken)
    {
        var parentSpaceId = await UnitOfWork.Set<ProjectFolder>()
            .AsNoTracking()
            .Where(f => f.Id == folderId)
            .Select(f => (Guid?)f.ProjectSpaceId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!parentSpaceId.HasValue)
            throw new KeyNotFoundException($"Folder not found: {folderId}");

        return (parentSpaceId.Value, EntityLayerType.ProjectSpace);
    }
}
