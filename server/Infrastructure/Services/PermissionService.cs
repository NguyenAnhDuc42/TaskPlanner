using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Common.Interfaces;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Errors.Model;

public class PermissionService : IPermissionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly HybridCache _cache;
    private readonly ILogger<PermissionService> _logger;
    private static readonly TimeSpan PermissionCacheDuration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan WorkspaceTraversalCacheDuration = TimeSpan.FromMinutes(30);

    public PermissionService(IUnitOfWork unitOfWork, HybridCache cache, ILogger<PermissionService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves an entity with a permission check using optimized single-query traversal.
    /// </summary>
    /// <typeparam name="T">The type of the entity that implements IHasWorkspaceId.</typeparam>
    /// <param name="entityId">The ID of the entity.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="requiredPermission">The required permission.</param>
    /// <param name="includeFunc">Optional include function for eager loading related entities.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The retrieved entity.</returns>
    /// <exception cref="NotFoundException">Thrown if the entity is not found.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if the user does not have the required permissions.</exception>
    public async Task<(T Entity, Guid WorkspaceId)> GetEntityWithPermissionAsync<T>(Guid entityId,Guid userId,Permission requiredPermission,Func<IQueryable<T>, IQueryable<T>>? includeFunc = null,CancellationToken ct = default) where T : class
    {
        var workspaceId = await GetProjectWorkspaceIdForEntityAsync<T>(entityId, ct);
        await EnsurePermissionAsync(userId, workspaceId, requiredPermission, ct);

        var query = _unitOfWork.Set<T>().AsNoTracking();
        if (includeFunc != null) query = includeFunc(query);

        var entity = await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == entityId, ct)
            ?? throw new NotFoundException($"{typeof(T).Name} {entityId} not found");

        return (entity, workspaceId);
    }

    public async Task EnsurePermissionAsync(Guid userId, Guid workspaceId, Permission permission, CancellationToken ct = default)
    {
        if (!await HasPermissionAsync(userId, workspaceId, permission, ct))
        {
            _logger.LogWarning("User {UserId} denied access to workspace {WorkspaceId} for permission {Permission}",
                userId, workspaceId, permission);
            throw new UnauthorizedAccessException($"User does not have {permission} permission for this workspace");
        }
    }

    public async Task EnsurePermissionAsync(Guid userId, Guid workspaceId, Permission[] permissions, CancellationToken ct = default)
    {
        if (!await HasPermissionAsync(userId, workspaceId, permissions, ct))
        {
            _logger.LogWarning("User {UserId} denied access to workspace {WorkspaceId} for permissions {Permissions}",
                userId, workspaceId, string.Join(", ", permissions));
            throw new UnauthorizedAccessException($"User does not have required permissions for this workspace");
        }
    }

    public async Task<bool> HasPermissionAsync(Guid userId, Guid workspaceId, Permission permission, CancellationToken ct = default)
    {
        var userPermissions = await GetUserPermissionsAsync(userId, workspaceId, ct);
        return (userPermissions & permission) == permission;
    }

    public async Task<bool> HasPermissionAsync(Guid userId, Guid workspaceId, Permission[] permissions, CancellationToken ct = default)
    {
        var userPermissions = await GetUserPermissionsAsync(userId, workspaceId, ct);
        return permissions.All(p => (userPermissions & p) == p);
    }

    /// <summary>
    /// Batch permission check for multiple entities - optimized for bulk operations
    /// </summary>
    public async Task<Dictionary<Guid, bool>> HasPermissionBatchAsync(
        Guid userId,
        IEnumerable<(Type entityType, Guid entityId)> entities,
        Permission permission,
        CancellationToken ct = default)
    {
        var entityList = entities.ToList();
        var results = new Dictionary<Guid, bool>();

        // Group by entity type for optimized queries
        var groupedEntities = entityList.GroupBy(e => e.entityType);

        foreach (var group in groupedEntities)
        {
            var entityIds = group.Select(g => g.entityId).ToList();
            var workspaceIds = await GetProjectWorkspaceIdBatchAsync(group.Key, entityIds, ct);

            foreach (var (entityId, workspaceId) in workspaceIds)
            {
                results[entityId] = await HasPermissionAsync(userId, workspaceId, permission, ct);
            }
        }

        return results;
    }

    // --- Permissions retrieval with caching ---
    public async Task<Permission> GetUserPermissionsAsync(Guid userId, Guid workspaceId, CancellationToken ct = default)
    {
        var cacheKey = $"user_permissions_{userId}_{workspaceId}";

        return await _cache.GetOrCreateAsync(cacheKey, async token =>
        {
            var membership = await _unitOfWork.Set<UserProjectWorkspace>()
                .AsNoTracking()
                .Include(m => m.ProjectWorkspace)
                .FirstOrDefaultAsync(m => m.UserId == userId && m.ProjectWorkspaceId == workspaceId, token);

            if (membership == null)
                return Permission.None;

            // Creator has all permissions regardless of role
            if (membership.ProjectWorkspace.CreatorId == userId)
                return Permission.All;

            return GetRolePermissions(membership.Role);

        }, new HybridCacheEntryOptions { Expiration = PermissionCacheDuration });
    }

    public async Task<IEnumerable<Guid>> GetUserAccessibleWorkspacesAsync(Guid userId, Permission permission, CancellationToken ct = default)
    {
        // Use raw SQL for better performance on large datasets
        var sql = @"
            SELECT upw.ProjectWorkspaceId
            FROM UserProjectWorkspaces upw
            INNER JOIN ProjectWorkspaces pw ON upw.ProjectWorkspaceId = pw.Id
            WHERE upw.UserId = @userId
            AND (pw.CreatorId = @userId OR upw.Role IN @allowedRoles)";

        var allowedRoles = GetRolesWithPermission(permission);

        return await _unitOfWork.QueryAsync<Guid>(sql, new { userId, allowedRoles }, ct);
    }

    public async Task<bool> IsWorkspaceOwnerAsync(Guid userId, Guid workspaceId, CancellationToken ct = default)
    {
        var role = await GetUserRoleAsync(userId, workspaceId, ct);
        return role == Role.Owner;
    }

    public async Task<bool> IsWorkspaceCreatorAsync(Guid userId, Guid workspaceId, CancellationToken ct = default)
    {
        var workspace = await _unitOfWork.ProjectWorkspaces.Query
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workspaceId, ct);

        return workspace?.CreatorId == userId;
    }

    public async Task<Role?> GetUserRoleAsync(Guid userId, Guid workspaceId, CancellationToken ct = default)
    {
        var membership = await _unitOfWork.Set<UserProjectWorkspace>()
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.ProjectWorkspaceId == workspaceId, ct);

        return membership?.Role;
    }

    public async Task<Guid> GetProjectWorkspaceIdForEntityAsync<T>(Guid entityId, CancellationToken ct = default)
    {
        var cacheKey = $"workspace_lookup_{typeof(T).Name}_{entityId}";

        return await _cache.GetOrCreateAsync(cacheKey, async token =>
        {
            return await ExecuteWorkspaceTraversalQuery<T>(entityId, token);
        }, new HybridCacheEntryOptions { Expiration = WorkspaceTraversalCacheDuration });
    }

    /// <summary>
    /// Batch version of workspace ID lookup for bulk operations
    /// </summary>
    public async Task<Dictionary<Guid, Guid>> GetProjectWorkspaceIdBatchAsync(Type entityType, IEnumerable<Guid> entityIds, CancellationToken ct = default)
    {
        var entityIdList = entityIds.ToList();
        if (!entityIdList.Any()) return new Dictionary<Guid, Guid>();

        var sql = GetBatchWorkspaceTraversalQuery(entityType);
        var parameters = new { entityIds = entityIdList };

        var results = await _unitOfWork.QueryAsync<(Guid EntityId, Guid WorkspaceId)>(sql, parameters, ct);
        return results.ToDictionary(r => r.EntityId, r => r.WorkspaceId);
    }

    private async Task<Guid> ExecuteWorkspaceTraversalQuery<T>(Guid entityId, CancellationToken ct)
    {
        var sql = GetWorkspaceTraversalQuery(typeof(T));
        var workspaceId = await _unitOfWork.QuerySingleOrDefaultAsync<Guid?>(sql, new { entityId }, ct);

        return workspaceId ?? throw new NotFoundException($"{typeof(T).Name} with ID {entityId} not found or has no valid workspace");
    }

    private static string GetWorkspaceTraversalQuery(Type entityType) => entityType.Name switch
    {
        nameof(ProjectTask) => @"
            SELECT pw.Id 
            FROM ProjectTasks pt
            INNER JOIN ProjectLists pl ON pt.ProjectListId = pl.Id
            LEFT JOIN ProjectFolders pf ON pl.ProjectFolderId = pf.Id
            INNER JOIN ProjectSpaces ps ON COALESCE(pf.ProjectSpaceId, pl.ProjectSpaceId) = ps.Id
            INNER JOIN ProjectWorkspaces pw ON ps.ProjectWorkspaceId = pw.Id
            WHERE pt.Id = @entityId",

        nameof(ProjectList) => @"
            SELECT pw.Id
            FROM ProjectLists pl
            LEFT JOIN ProjectFolders pf ON pl.ProjectFolderId = pf.Id
            INNER JOIN ProjectSpaces ps ON COALESCE(pf.ProjectSpaceId, pl.ProjectSpaceId) = ps.Id
            INNER JOIN ProjectWorkspaces pw ON ps.ProjectWorkspaceId = pw.Id
            WHERE pl.Id = @entityId",

        nameof(ProjectFolder) => @"
            SELECT pw.Id
            FROM ProjectFolders pf
            INNER JOIN ProjectSpaces ps ON pf.ProjectSpaceId = ps.Id
            INNER JOIN ProjectWorkspaces pw ON ps.ProjectWorkspaceId = pw.Id
            WHERE pf.Id = @entityId",

        nameof(ProjectSpace) => @"
            SELECT pw.Id
            FROM ProjectSpaces ps
            INNER JOIN ProjectWorkspaces pw ON ps.ProjectWorkspaceId = pw.Id
            WHERE ps.Id = @entityId",

        nameof(ProjectWorkspace) => @"
            SELECT Id FROM ProjectWorkspaces WHERE Id = @entityId",

        _ => throw new NotSupportedException($"Workspace traversal not implemented for entity type: {entityType.Name}")
    };

    private static string GetBatchWorkspaceTraversalQuery(Type entityType) => entityType.Name switch
    {
        nameof(ProjectTask) => @"
            SELECT pt.Id as EntityId, pw.Id as WorkspaceId
            FROM ProjectTasks pt
            INNER JOIN ProjectLists pl ON pt.ProjectListId = pl.Id
            LEFT JOIN ProjectFolders pf ON pl.ProjectFolderId = pf.Id
            INNER JOIN ProjectSpaces ps ON COALESCE(pf.ProjectSpaceId, pl.ProjectSpaceId) = ps.Id
            INNER JOIN ProjectWorkspaces pw ON ps.ProjectWorkspaceId = pw.Id
            WHERE pt.Id = ANY(@entityIds)",

        nameof(ProjectList) => @"
            SELECT pl.Id as EntityId, pw.Id as WorkspaceId
            FROM ProjectLists pl
            LEFT JOIN ProjectFolders pf ON pl.ProjectFolderId = pf.Id
            INNER JOIN ProjectSpaces ps ON COALESCE(pf.ProjectSpaceId, pl.ProjectSpaceId) = ps.Id
            INNER JOIN ProjectWorkspaces pw ON ps.ProjectWorkspaceId = pw.Id
            WHERE pl.Id = ANY(@entityIds)",

        nameof(ProjectFolder) => @"
            SELECT pf.Id as EntityId, pw.Id as WorkspaceId
            FROM ProjectFolders pf
            INNER JOIN ProjectSpaces ps ON pf.ProjectSpaceId = ps.Id
            INNER JOIN ProjectWorkspaces pw ON ps.ProjectWorkspaceId = pw.Id
            WHERE pf.Id = ANY(@entityIds)",

        nameof(ProjectSpace) => @"
            SELECT ps.Id as EntityId, pw.Id as WorkspaceId
            FROM ProjectSpaces ps
            INNER JOIN ProjectWorkspaces pw ON ps.ProjectWorkspaceId = pw.Id
            WHERE ps.Id = ANY(@entityIds)",

        _ => throw new NotSupportedException($"Batch workspace traversal not implemented for entity type: {entityType.Name}")
    };

    private static Permission GetRolePermissions(Role role) => role switch
    {
        Role.Owner => Permission.Owner_Permissions,

        Role.Admin => Permission.Workspace_Admin | Permission.Member_Admin | Permission.Content_Admin |
                        Permission.View_Reports | Permission.Export_Data |
                        Permission.View_Comments | Permission.Create_Comments | Permission.Edit_All_Comments | Permission.Delete_All_Comments |
                        Permission.View_Attachments | Permission.Upload_Attachments | Permission.Delete_All_Attachments,

        Role.Member => Permission.View_Workspace | Permission.View_Members | Permission.View_Spaces |
                        Permission.Create_Spaces | Permission.Edit_Spaces | Permission.Archive_Spaces |
                        Permission.View_Lists | Permission.Create_Lists | Permission.Edit_Lists | Permission.Reorder_Lists |
                        Permission.View_Tasks | Permission.Create_Tasks | Permission.Edit_Tasks | Permission.Assign_Tasks | Permission.Change_Task_Status |
                        Permission.View_Statuses | Permission.Create_Statuses | Permission.Edit_Statuses |
                        Permission.View_Comments | Permission.Create_Comments | Permission.Edit_Own_Comments | Permission.Delete_Own_Comments |
                        Permission.View_Attachments | Permission.Upload_Attachments | Permission.Delete_Own_Attachments,

        Role.Guest => Permission.View_Workspace | Permission.View_Members | Permission.View_Spaces |
                        Permission.View_Lists | Permission.View_Tasks | Permission.View_Statuses |
                        Permission.View_Comments | Permission.View_Attachments,

        _ => Permission.None
    };

    private static IEnumerable<Role> GetRolesWithPermission(Permission permission)
    {
        var roles = new List<Role>();

        foreach (Role role in Enum.GetValues<Role>())
        {
            if ((GetRolePermissions(role) & permission) == permission)
            {
                roles.Add(role);
            }
        }

        return roles;
    }

    /// <summary>
    /// Invalidates cached permissions for a user in a specific workspace
    /// Call this when user roles change
    /// </summary>
    public async Task InvalidateUserPermissionCacheAsync(Guid userId, Guid workspaceId)
    {
        var cacheKey = $"user_permissions_{userId}_{workspaceId}";
        await _cache.RemoveAsync(cacheKey);
    }

    /// <summary>
    /// Invalidates cached workspace traversal for an entity
    /// Call this when entity hierarchy changes (moves, deletions, etc.)
    /// </summary>
    public async Task InvalidateWorkspaceTraversalCacheAsync<T>(Guid entityId)
    {
        var cacheKey = $"workspace_lookup_{typeof(T).Name}_{entityId}";
        await _cache.RemoveAsync(cacheKey);
    }

    /// <summary>
    /// Invalidates all cached workspace traversals for entities that may be affected by hierarchy changes
    /// Call this when moving spaces, folders, or when workspace structure changes
    /// </summary>
    public async Task InvalidateHierarchyTraversalCacheAsync(Guid parentEntityId, Type parentEntityType)
    {
        // This would require more sophisticated cache invalidation patterns
        // For now, implement based on your specific cache invalidation strategy
        _logger.LogInformation("Hierarchy change detected for {EntityType} {EntityId} - consider implementing pattern-based cache invalidation",
            parentEntityType.Name, parentEntityId);
    }
}