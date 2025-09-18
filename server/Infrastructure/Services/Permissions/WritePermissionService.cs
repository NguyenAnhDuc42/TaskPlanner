using System;
using Application.Common.Results;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities.Relationship;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Permissions;

public class WritePermissionService
{
    private readonly IUnitOfWork _unitOfWork;
    private HybridCache _cache;
    private readonly ILogger<WritePermissionService> _logger;
    private readonly IWorkspacePermissionService _workspaceService;
    private readonly TimeSpan _cacheTime = TimeSpan.FromMinutes(5);

    public WritePermissionService(IUnitOfWork unitOfWork, HybridCache cache, ILogger<WritePermissionService> logger, IWorkspacePermissionService workspaceService)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
        _workspaceService = workspaceService;
    }

    private static string UserAccessCacheKey(EntityType entityType, Guid entityId, Guid userId) => $"entity:{(int)entityType}:{entityId}:user:{userId}:grant";

    public async Task<WritePermissionResult> CheckWritePermissionAsync(Guid userId, EntityType entityType, Guid entityId, Permission requiredPermission, Guid workspaceId, CancellationToken ct = default)
    {
        try
        {
            var userAcess = await GetUserAccess(userId, entityType, entityId, ct);
            if (userAcess != null)
            {
                var permission = PermissionHelper.MapAccessLevelToPermissionMask(entityType, userAcess.AccessLevel);
                if (PermissionHelper.MaskHas(permission, requiredPermission)) return new WritePermissionResult(true, "Have Permission to execute action");
                _logger.LogDebug("Entity-level access insufficient. Required: {Required}, Has: {Has}",
                       requiredPermission, permission);
            }
            var worksapaceRole = await _workspaceService.CheckForUser(workspaceId, userId,ct);
            if (worksapaceRole != Role.None)
            {
                var rolePermission = PermissionHelper.MapRoleToPermissionMask(worksapaceRole);
                if (PermissionHelper.MaskHas(rolePermission, requiredPermission)) return new WritePermissionResult(true, "Have Permission to execute action");
                _logger.LogDebug("Workspace role access insufficient. Required: {Required}, Has: {Has}",
                       requiredPermission, rolePermission);
            }

            return new WritePermissionResult(false, "Insufficient permissions");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check permissions for User: {UserId}, Entity: {EntityType}:{EntityId}", 
                userId, entityType, entityId);
            return new WritePermissionResult(false,"Permission check failed");
        }

    }


    private async Task<UserAccessLayer?> GetUserAccess(Guid userId, EntityType entityType, Guid entityId, CancellationToken ct = default)
    {
        var cacheKey = UserAccessCacheKey(entityType, entityId, userId);

        var cacheResult = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            var userAcess = await _unitOfWork.Set<UserAccessLayer>().Where(ua => ua.UserId == userId && ua.EntityType == entityType && ua.EntityId == entityId).FirstOrDefaultAsync();
            return userAcess;
        },
        new HybridCacheEntryOptions
        {
            Expiration = _cacheTime,

        });
        return cacheResult;
    }


}
