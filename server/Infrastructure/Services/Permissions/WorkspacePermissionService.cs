using System;
using Application.Interfaces.Repositories;
using Domain.Entities.Relationship;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Permissions;

public class WorkspacePermissionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly HybridCache _cache;
    private readonly ILogger<WorkspacePermissionService> _logger;
    private readonly TimeSpan _cacheTime = TimeSpan.FromMinutes(5);



    public WorkspacePermissionService(IUnitOfWork unitOfWork, HybridCache cache, ILogger<WorkspacePermissionService> logger)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }
    private static string WorkspaceRoleCacheKey(Guid workspaceId, Guid userId) => $"workspace:{workspaceId}:user:{userId}:role";

    public async Task<Role> CheckForUser(Guid workspaceId, Guid userId, CancellationToken ct = default)
    {
        var cacheKey = WorkspaceRoleCacheKey(workspaceId, userId);

        var cacheRole = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {

            var member = await _unitOfWork.Set<WorkspaceMember>().Where(wm => wm.ProjectWorkspaceId == workspaceId && wm.UserId == userId).FirstOrDefaultAsync(ct);
            if (member == null)
            {
                _logger.LogWarning("Permission check failed: User {UserId} not in Workspace {WorkspaceId}", userId, workspaceId);
                return Role.None;
            }
            return member.Role;
        },
        new HybridCacheEntryOptions
        {
            Expiration = _cacheTime,
        });
        return cacheRole;

    }
    public async Task InvalidateUserRoleAsync(Guid workspaceId, Guid userId)
    {
        var cacheKey = WorkspaceRoleCacheKey(workspaceId, userId);
        await _cache.RemoveAsync(cacheKey);
        _logger.LogInformation("Cache invalidated for User {UserId} in Workspace {WorkspaceId}", userId, workspaceId);
    }


}
