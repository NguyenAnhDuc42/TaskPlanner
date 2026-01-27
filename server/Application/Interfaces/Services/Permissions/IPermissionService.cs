using System;
using Domain.Common;
using Domain.Common.Interfaces;
using Domain.Enums;

namespace Application.Interfaces.Services.Permissions;

public interface IPermissionService
{
    Task<bool> CanPerformAsync<TEntity>(
        Guid workspaceId,
        Guid userId,
        TEntity entity,
        PermissionAction action,
        CancellationToken ct)
        where TEntity : Entity;

    Task<bool> CanPerformAsync(
        Guid workspaceId,
        Guid userId,
        Guid entityId,
        EntityType type,
        PermissionAction action,
        CancellationToken ct);

    // Overload 2: Create child in parent
    Task<bool> CanPerformAsync<TParent>(
        Guid workspaceId,
        Guid userId,
        TParent parentEntity,
        EntityType childType,
        PermissionAction action,
        CancellationToken ct)
        where TParent : Entity;

    // Overload 3: Action on child with parent context
    Task<bool> CanPerformAsync<TChild, TParent>(
        Guid workspaceId,
        Guid userId,
        TChild childEntity,
        TParent parentEntity,
        PermissionAction action,
        CancellationToken ct)
        where TChild : Entity
        where TParent : Entity;

    // Cache invalidation methods
    Task InvalidateWorkspaceRoleCacheAsync(Guid userId, Guid workspaceId);
    Task InvalidateEntityAccessCacheAsync(Guid userId, Guid entityId, EntityType entityType);
    Task InvalidateChatRoomCacheAsync(Guid userId, Guid chatRoomId);
    Task InvalidateUserCacheAsync(Guid userId, Guid workspaceId);
}
