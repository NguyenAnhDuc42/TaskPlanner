using Domain.Common;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Application.Interfaces.Services.Permissions;

namespace Infrastructure.Services.Permissions;

public class PermissionService : IPermissionService
{
    private readonly IHierarchyService _hierarchyService;
    private readonly PermissionDataFetcher _dataFetcher;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        IHierarchyService hierarchyService,
        PermissionDataFetcher dataFetcher,
        ILogger<PermissionService> logger)
    {
        _hierarchyService = hierarchyService;
        _dataFetcher = dataFetcher;
        _logger = logger;
    }

    public async Task<bool> CanPerformAsync<TEntity>(
        Guid workspaceId,
        Guid userId,
        TEntity entity,
        PermissionAction action,
        CancellationToken ct) where TEntity : Entity
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        var entityType = PermissionDataFetcher.GetEntityType<TEntity>();
        
        return await CanPerformAsync(workspaceId, userId, entity.Id, entityType, action, ct);
    }

    public async Task<bool> CanPerformAsync(
        Guid workspaceId,
        Guid userId,
        Guid entityId,
        EntityType type,
        PermissionAction action,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // 1. Resolve Hierarchy
        var path = await _hierarchyService.ResolvePathAsync(entityId, type, ct);
        if (!path.IsValid)
        {
            _logger.LogWarning("Failed to resolve hierarchy for {Type} {Id}", type, entityId);
            return false;
        }

        // 2. Validate Workspace Boundary
        if (path.WorkspaceId != workspaceId)
        {
            _logger.LogWarning("Security violation: Entity {Id} ({Type}) belongs to workspace {ActualWs}, but request for {RequestedWs}",
                entityId, type, path.WorkspaceId, workspaceId);
            return false;
        }

        // 3. Get Security Context (The Waterfall)
        var context = await _dataFetcher.GetSecurityContextAsync(userId, path, ct);

        // 4. Matrix Check
        var result = PermissionMatrix.CanPerform(type, action, context);

        if (!result)
        {
            _logger.LogWarning("Permission denied: User {UserId} action {Action} on {Type} {EntityId}",
                userId, action, type, entityId);
        }

        return result;
    }

    // Gradual refactor of older overloads to use the same logic if possible
    public async Task<bool> CanPerformAsync<TParent>(
        Guid workspaceId,
        Guid userId,
        TParent parentEntity,
        EntityType childType,
        PermissionAction action,
        CancellationToken ct) where TParent : Entity
    {
        if (parentEntity == null) throw new ArgumentNullException(nameof(parentEntity));
        // For creating children, the ID we check is the PARENT's ID
        return await CanPerformAsync(workspaceId, userId, parentEntity.Id, PermissionDataFetcher.GetEntityType<TParent>(), action, ct);
    }

    public async Task<bool> CanPerformAsync<TChild, TParent>(Guid workspaceId, Guid userId, TChild childEntity, TParent parentEntity, PermissionAction action, CancellationToken ct)
        where TChild : Entity
        where TParent : Entity
    {
        if (childEntity == null) throw new ArgumentNullException(nameof(childEntity));
        return await CanPerformAsync(workspaceId, userId, childEntity.Id, PermissionDataFetcher.GetEntityType<TChild>(), action, ct);
    }

    public async Task InvalidateWorkspaceRoleCacheAsync(Guid userId, Guid workspaceId)
    {
        await _dataFetcher.InvalidateWorkspaceRoleCacheAsync(userId, workspaceId);
    }

    public async Task InvalidateEntityAccessCacheAsync(Guid userId, Guid entityId, EntityType entityType)
    {
        await _dataFetcher.InvalidateUserPermissionsCacheAsync(userId);
    }

    public async Task InvalidateChatRoomCacheAsync(Guid userId, Guid chatRoomId)
    {
        await _dataFetcher.InvalidateChatRoomCacheAsync(userId, chatRoomId);
    }

    public async Task InvalidateUserCacheAsync(Guid userId, Guid workspaceId)
    {
        await _dataFetcher.InvalidateUserPermissionsCacheAsync(userId);
        await _dataFetcher.InvalidateWorkspaceRoleCacheAsync(userId, workspaceId);
    }
}