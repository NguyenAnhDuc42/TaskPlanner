using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Dapper;
using Domain.Common.Interfaces;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Errors.Model;

public interface IWorkspaceOwned
{
    Guid ProjectWorkspaceId { get; }
}

public class PermissionService : IPermissionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDbConnection _dbConnection;
    private readonly HybridCache _cache;
    private readonly ILogger<PermissionService> _logger;
    private static readonly TimeSpan PermissionCacheDuration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan WorkspaceTraversalCacheDuration = TimeSpan.FromMinutes(30);

    // Define which per-entity permissions the creator gets for their own entity (conservative).
    private static readonly Permission CreatorElevatedPermissions =
          Permission.Edit_Tasks
        | Permission.Delete_Tasks
        | Permission.Edit_Own_Comments
        | Permission.Delete_Own_Comments
        | Permission.Upload_Attachments
        | Permission.Delete_Own_Attachments;

    public PermissionService(IUnitOfWork unitOfWork, IDbConnection dbConnection, HybridCache cache, ILogger<PermissionService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------

    /// <summary>
    /// Generic method to fetch an entity only if the user has the required permission.
    /// Applies creator-elevation and visibility semantics via HasPermissionOnEntityAsync.
    /// Fast-path for IWorkspaceOwned (single EF projection).
    /// </summary>
    public async Task<T> GetEntityWithPermissionAsync<T>(Guid entityId, Guid userId, Permission requiredPermission, Func<IQueryable<T>, IQueryable<T>>? includeFunc = null, CancellationToken ct = default) where T : class
    {
        // Fast-path: T exposes ProjectWorkspaceId
        if (typeof(IWorkspaceOwned).IsAssignableFrom(typeof(T)))
        {
            var query = _unitOfWork.Set<T>().AsNoTracking();
            if (includeFunc != null) query = includeFunc(query);

            // Project entity + workspace id + creator id (if present) in one query to reduce round-trips
            var row = await query
                .Where(e => EF.Property<Guid>(e, "Id") == entityId)
                .Select(e => new
                {
                    Entity = e,
                    WorkspaceId = EF.Property<Guid>(e, nameof(IWorkspaceOwned.ProjectWorkspaceId)),
                    CreatorId = EF.Property<Guid?>(e, "CreatorId")
                })
                .FirstOrDefaultAsync(ct);

            if (row == null) throw new NotFoundException($"{typeof(T).Name} {entityId} not found");

            // Use entity-level semantics (workspace role, creator-elevation, visibility)
            var allowed = await HasPermissionOnEntityAsync(typeof(T), entityId, userId, requiredPermission, ct);
            if (!allowed)
            {
                _logger.LogWarning("User {UserId} denied access to {EntityType}/{EntityId} for permission {Permission}", userId, typeof(T).Name, entityId, requiredPermission);
                throw new UnauthorizedAccessException($"User does not have {requiredPermission} permission for this resource");
            }

            return row.Entity;
        }

        // Fallback: use generic entity-level check (traversal + checks), then fetch entity
        var allowedFallback = await HasPermissionOnEntityAsync(typeof(T), entityId, userId, requiredPermission, ct);
        if (!allowedFallback)
        {
            _logger.LogWarning("User {UserId} denied access to {EntityType}/{EntityId} for permission {Permission}", userId, typeof(T).Name, entityId, requiredPermission);
            throw new UnauthorizedAccessException($"User does not have {requiredPermission} permission for this resource");
        }

        var fetchQuery = _unitOfWork.Set<T>().AsNoTracking();
        if (includeFunc != null) fetchQuery = includeFunc(fetchQuery);

        return await fetchQuery.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == entityId, ct)
            ?? throw new NotFoundException($"{typeof(T).Name} {entityId} not found");
    }

    /// <summary>
    /// Workspace-only permission enforcement (role-based). Throws UnauthorizedAccessException if missing.
    /// </summary>
    public async Task EnsurePermissionAsync(Guid userId, Guid workspaceId, Permission permission, CancellationToken ct = default)
    {
        if (!await HasPermissionAsync(userId, workspaceId, permission, ct))
        {
            _logger.LogWarning("User {UserId} denied access to workspace {WorkspaceId} for permission {Permission}", userId, workspaceId, permission);
            throw new UnauthorizedAccessException($"User does not have {permission} permission for this workspace");
        }
    }

    public async Task EnsurePermissionAsync(Guid userId, Guid workspaceId, Permission[] permissions, CancellationToken ct = default)
    {
        if (!await HasPermissionAsync(userId, workspaceId, permissions, ct))
        {
            _logger.LogWarning("User {UserId} denied access to workspace {WorkspaceId} for permissions {Permissions}", userId, workspaceId, string.Join(", ", permissions));
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
    /// Returns cached user permissions for a workspace (role-based). Creator of workspace receives owner mask.
    /// </summary>
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
            {
                // If no membership, check if user is workspace creator (fallback)
                var pw = await _unitOfWork.ProjectWorkspaces.Query
                    .AsNoTracking()
                    .Select(p => new { p.Id, p.CreatorId })
                    .FirstOrDefaultAsync(p => p.Id == workspaceId, token);

                if (pw?.CreatorId == userId) return PermissionHelper.GetOwnerPermissions();
                return Permission.None;
            }

            // If membership is present and nav loaded: workspace creator -> owner permissions
            if (membership.ProjectWorkspace != null && membership.ProjectWorkspace.CreatorId == userId)
                return PermissionHelper.GetOwnerPermissions();

            // Standard mapping from role to permission mask
            return GetRolePermissions(membership.Role);

        }, new HybridCacheEntryOptions { Expiration = PermissionCacheDuration });
    }

    public async Task<IEnumerable<Guid>> GetUserAccessibleWorkspacesAsync(Guid userId, Permission permission, CancellationToken ct = default)
    {
        var allowedRoles = GetRolesWithPermission(permission).Cast<int>().ToArray();
        var allowPublic = PermissionHelper.IsViewPermission(permission); // view-like permissions allowed publicly by default
        var publicVisibility = (int)Visibility.Public;

        var sql = @"
            SELECT pw.""Id""
            FROM ""ProjectWorkspaces"" pw
            LEFT JOIN ""UserProjectWorkspaces"" upw
                ON upw.""ProjectWorkspaceId"" = pw.""Id"" AND upw.""UserId"" = @userId
            WHERE
                pw.""CreatorId"" = @userId
                OR (upw.""Role"" = ANY(@allowedRoles))
                OR (pw.""Visibility"" = @publicVisibility AND @allowPublic = TRUE)
        ";

        return await _dbConnection.QueryAsync<Guid>(sql, new { userId, allowedRoles, publicVisibility, allowPublic });
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

    // ------------------------------------------------------------
    // Traversal & batch helpers
    // ------------------------------------------------------------

    public async Task<Guid> GetProjectWorkspaceIdForEntityAsync<T>(Guid entityId, CancellationToken ct = default)
    {
        var cacheKey = $"workspace_lookup_{typeof(T).Name}_{entityId}";

        return await _cache.GetOrCreateAsync(cacheKey, async token =>
        {
            return await ExecuteWorkspaceTraversalQuery<T>(entityId, token);
        }, new HybridCacheEntryOptions { Expiration = WorkspaceTraversalCacheDuration });
    }

    public async Task<Dictionary<Guid, Guid>> GetProjectWorkspaceIdBatchAsync(Type entityType, IEnumerable<Guid> entityIds, CancellationToken ct = default)
    {
        var entityIdList = entityIds.ToList();
        if (!entityIdList.Any()) return new Dictionary<Guid, Guid>();

        if (entityType == typeof(ProjectWorkspace))
        {
            var sqlExists = @"
                SELECT pw.""Id"" as EntityId, pw.""Id"" as WorkspaceId
                FROM ""ProjectWorkspaces"" pw
                WHERE pw.""Id"" = ANY(@entityIds)";
            var rows = await _dbConnection.QueryAsync<(Guid EntityId, Guid WorkspaceId)>(sqlExists, new { entityIds = entityIdList.ToArray() });
            return rows.ToDictionary(r => r.EntityId, r => r.WorkspaceId);
        }

        var sql = GetBatchWorkspaceTraversalQuery(entityType);
        var parameters = new { entityIds = entityIdList.ToArray() };

        var results = await _dbConnection.QueryAsync<(Guid EntityId, Guid WorkspaceId)>(sql, parameters);
        return results.ToDictionary(r => r.EntityId, r => r.WorkspaceId);
    }

    private async Task<Guid> ExecuteWorkspaceTraversalQuery<T>(Guid entityId, CancellationToken ct)
    {
        var sql = GetWorkspaceTraversalQuery(typeof(T));
        var workspaceId = await _dbConnection.QuerySingleOrDefaultAsync<Guid?>(sql, new { entityId });

        return workspaceId ?? throw new NotFoundException($"{typeof(T).Name} with ID {entityId} not found or has no valid workspace");
    }

    private static string GetWorkspaceTraversalQuery(Type entityType) => entityType.Name switch
    {
        nameof(ProjectTask) => @"
            SELECT pw.""Id"" 
            FROM ""ProjectTasks"" pt
            INNER JOIN ""ProjectLists"" pl ON pt.""ProjectListId"" = pl.""Id""
            LEFT JOIN ""ProjectFolders"" pf ON pl.""ProjectFolderId"" = pf.""Id""
            INNER JOIN ""ProjectSpaces"" ps ON COALESCE(pf.""ProjectSpaceId"", pl.""ProjectSpaceId"") = ps.""Id""
            INNER JOIN ""ProjectWorkspaces"" pw ON ps.""ProjectWorkspaceId"" = pw.""Id""
            WHERE pt.""Id"" = @entityId",

        nameof(ProjectList) => @"
            SELECT pw.""Id""
            FROM ""ProjectLists"" pl
            LEFT JOIN ""ProjectFolders"" pf ON pl.""ProjectFolderId"" = pf.""Id""
            INNER JOIN ""ProjectSpaces"" ps ON COALESCE(pf.""ProjectSpaceId"", pl.""ProjectSpaceId"") = ps.""Id""
            INNER JOIN ""ProjectWorkspaces"" pw ON ps.""ProjectWorkspaceId"" = pw.""Id""
            WHERE pl.""Id"" = @entityId",

        nameof(ProjectFolder) => @"
            SELECT pw.""Id""
            FROM ""ProjectFolders"" pf
            INNER JOIN ""ProjectSpaces"" ps ON pf.""ProjectSpaceId"" = ps.""Id""
            INNER JOIN ""ProjectWorkspaces"" pw ON ps.""ProjectWorkspaceId"" = pw.""Id""
            WHERE pf.""Id"" = @entityId",

        nameof(ProjectSpace) => @"
            SELECT pw.""Id""
            FROM ""ProjectSpaces"" ps
            INNER JOIN ""ProjectWorkspaces"" pw ON ps.""ProjectWorkspaceId"" = pw.""Id""
            WHERE ps.""Id"" = @entityId",

        nameof(ProjectWorkspace) => @"
            SELECT ""Id"" FROM ""ProjectWorkspaces"" WHERE ""Id"" = @entityId",

        _ => throw new NotSupportedException($"Workspace traversal not implemented for entity type: {entityType.Name}")
    };

    private static string GetBatchWorkspaceTraversalQuery(Type entityType) => entityType.Name switch
    {
        nameof(ProjectTask) => @"
            SELECT pt.""Id"" as EntityId, pw.""Id"" as WorkspaceId
            FROM ""ProjectTasks"" pt
            INNER JOIN ""ProjectLists"" pl ON pt.""ProjectListId"" = pl.""Id""
            LEFT JOIN ""ProjectFolders"" pf ON pl.""ProjectFolderId"" = pf.""Id""
            INNER JOIN ""ProjectSpaces"" ps ON COALESCE(pf.""ProjectSpaceId"", pl.""ProjectSpaceId"") = ps.""Id""
            INNER JOIN ""ProjectWorkspaces"" pw ON ps.""ProjectWorkspaceId"" = pw.""Id""
            WHERE pt.""Id"" = ANY(@entityIds)",

        nameof(ProjectList) => @"
            SELECT pl.""Id"" as EntityId, pw.""Id"" as WorkspaceId
            FROM ""ProjectLists"" pl
            LEFT JOIN ""ProjectFolders"" pf ON pl.""ProjectFolderId"" = pf.""Id""
            INNER JOIN ""ProjectSpaces"" ps ON COALESCE(pf.""ProjectSpaceId"", pl.""ProjectSpaceId"") = ps.""Id""
            INNER JOIN ""ProjectWorkspaces"" pw ON ps.""ProjectWorkspaceId"" = pw.""Id""
            WHERE pl.""Id"" = ANY(@entityIds)",

        nameof(ProjectFolder) => @"
            SELECT pf.""Id"" as EntityId, pw.""Id"" as WorkspaceId
            FROM ""ProjectFolders"" pf
            INNER JOIN ""ProjectSpaces"" ps ON pf.""ProjectSpaceId"" = ps.""Id""
            INNER JOIN ""ProjectWorkspaces"" pw ON ps.""ProjectWorkspaceId"" = pw.""Id""
            WHERE pf.""Id"" = ANY(@entityIds)",

        nameof(ProjectSpace) => @"
            SELECT ps.""Id"" as EntityId, pw.""Id"" as WorkspaceId
            FROM ""ProjectSpaces"" ps
            INNER JOIN ""ProjectWorkspaces"" pw ON ps.""ProjectWorkspaceId"" = pw.""Id""
            WHERE ps.""Id"" = ANY(@entityIds)",

        _ => throw new NotSupportedException($"Batch workspace traversal not implemented for entity type: {entityType.Name}")
    };

    // ------------------------------------------------------------
    // Entity-level authorization & visibility support
    // ------------------------------------------------------------

    /// <summary>
    /// Entity-level authorization with precedence:
    /// 1) workspace creator -> allow
    /// 2) workspace role permissions -> allow
    /// 3) entity creator elevated permissions -> allow
    /// 4) visibility-based view allowance -> allow
    /// 5) deny
    /// </summary>
    public async Task<bool> HasPermissionOnEntityAsync(Type entityType, Guid entityId, Guid userId, Permission permission, CancellationToken ct = default)
    {
        // resolve workspace (generic)
        Guid workspaceId;
        try
        {
            workspaceId = await GetProjectWorkspaceIdForEntityAsyncGeneric(entityType, entityId, ct);
        }
        catch (NotFoundException)
        {
            return false;
        }

        // 1) workspace creator
        if (await IsWorkspaceCreatorAsync(userId, workspaceId, ct)) return true;

        // 2) workspace role permissions
        if (await HasPermissionAsync(userId, workspaceId, permission, ct)) return true;

        // 3) entity creator elevation
        var creator = await GetEntityCreatorAsync(entityType, entityId, ct);
        if (creator.HasValue && creator.Value == userId)
        {
            if ((CreatorElevatedPermissions & permission) == permission) return true;
        }

        // 4) visibility checks for view permissions
        if (PermissionHelper.IsViewPermission(permission))
        {
            var effectiveVisibility = await GetEffectiveVisibilityForEntityAsync(entityType, entityId, ct);
            return await EvaluateVisibilityAccessAsync(userId, workspaceId, effectiveVisibility, permission, ct);
        }

        return false;
    }

    private async Task<Guid> GetProjectWorkspaceIdForEntityAsyncGeneric(Type entityType, Guid entityId, CancellationToken ct = default)
    {
        var method = GetType().GetMethod(nameof(GetProjectWorkspaceIdForEntityAsync), BindingFlags.Public | BindingFlags.Instance);
        if (method == null) throw new InvalidOperationException("Traversal helper not found");
        var generic = method.MakeGenericMethod(entityType);
        var task = (Task)generic.Invoke(this, new object[] { entityId, ct })!;
        await task.ConfigureAwait(false);
        var resultProperty = task.GetType().GetProperty("Result");
        return (Guid)resultProperty!.GetValue(task)!;
    }

    private async Task<bool> EvaluateVisibilityAccessAsync(Guid userId, Guid workspaceId, Visibility visibility, Permission permission, CancellationToken ct = default)
    {
        switch (visibility)
        {
            case Visibility.Public:
                // public view allowed
                return true;
            case Visibility.Restricted:
                var role = await GetUserRoleAsync(userId, workspaceId, ct);
                return role != null || await IsWorkspaceCreatorAsync(userId, workspaceId, ct);
            case Visibility.Private:
            default:
                return false;
        }
    }

    private async Task<Guid?> GetEntityCreatorAsync(Type entityType, Guid entityId, CancellationToken ct = default)
    {
        switch (entityType.Name)
        {
            case nameof(ProjectTask):
                return await _dbConnection.QuerySingleOrDefaultAsync<Guid?>(@"SELECT ""CreatorId"" FROM ""ProjectTasks"" WHERE ""Id"" = @id", new { id = entityId });
            case nameof(ProjectList):
                return await _dbConnection.QuerySingleOrDefaultAsync<Guid?>(@"SELECT ""CreatorId"" FROM ""ProjectLists"" WHERE ""Id"" = @id", new { id = entityId });
            case nameof(ProjectFolder):
                return await _dbConnection.QuerySingleOrDefaultAsync<Guid?>(@"SELECT ""CreatorId"" FROM ""ProjectFolders"" WHERE ""Id"" = @id", new { id = entityId });
            case nameof(ProjectSpace):
                return await _dbConnection.QuerySingleOrDefaultAsync<Guid?>(@"SELECT ""CreatorId"" FROM ""ProjectSpaces"" WHERE ""Id"" = @id", new { id = entityId });
            case nameof(ProjectWorkspace):
                return await _dbConnection.QuerySingleOrDefaultAsync<Guid?>(@"SELECT ""CreatorId"" FROM ""ProjectWorkspaces"" WHERE ""Id"" = @id", new { id = entityId });
            default:
                throw new NotSupportedException($"GetEntityCreatorAsync not implemented for {entityType.Name}");
        }
    }

    private async Task<Visibility> GetEffectiveVisibilityForEntityAsync(Type entityType, Guid entityId, CancellationToken ct = default)
    {
        switch (entityType.Name)
        {
            case nameof(ProjectTask):
                var sqlTask =
                    @"SELECT COALESCE(pl.""Visibility"", pf.""Visibility"", ps.""Visibility"", pw.""Visibility"") as Visibility
                    FROM ""ProjectTasks"" pt
                    INNER JOIN ""ProjectLists"" pl ON pt.""ProjectListId"" = pl.""Id""
                    LEFT JOIN ""ProjectFolders"" pf ON pl.""ProjectFolderId"" = pf.""Id""
                    INNER JOIN ""ProjectSpaces"" ps ON COALESCE(pf.""ProjectSpaceId"", pl.""ProjectSpaceId"") = ps.""Id""
                    INNER JOIN ""ProjectWorkspaces"" pw ON ps.""ProjectWorkspaceId"" = pw.""Id""
                    WHERE pt.""Id"" = @id";
                var raw = await _dbConnection.QuerySingleOrDefaultAsync<int?>(sqlTask, new { id = entityId });
                return raw.HasValue ? (Visibility)raw.Value : Visibility.Private;

            case nameof(ProjectList):
                var sqlList =
                    @"SELECT COALESCE(pl.""Visibility"", pf.""Visibility"", ps.""Visibility"", pw.""Visibility"") as Visibility
                    FROM ""ProjectLists"" pl
                    LEFT JOIN ""ProjectFolders"" pf ON pl.""ProjectFolderId"" = pf.""Id""
                    INNER JOIN ""ProjectSpaces"" ps ON COALESCE(pf.""ProjectSpaceId"", pl.""ProjectSpaceId"") = ps.""Id""
                    INNER JOIN ""ProjectWorkspaces"" pw ON ps.""ProjectWorkspaceId"" = pw.""Id""
                    WHERE pl.""Id"" = @id";
                var rv2 = await _dbConnection.QuerySingleOrDefaultAsync<int?>(sqlList, new { id = entityId });
                return rv2.HasValue ? (Visibility)rv2.Value : Visibility.Private;

            case nameof(ProjectFolder):
                var sqlFolder =
                    @"SELECT COALESCE(pf.""Visibility"", ps.""Visibility"", pw.""Visibility"") as Visibility
                    FROM ""ProjectFolders"" pf
                    INNER JOIN ""ProjectSpaces"" ps ON pf.""ProjectSpaceId"" = ps.""Id""
                    INNER JOIN ""ProjectWorkspaces"" pw ON ps.""ProjectWorkspaceId"" = pw.""Id""
                    WHERE pf.""Id"" = @id";
                var rv3 = await _dbConnection.QuerySingleOrDefaultAsync<int?>(sqlFolder, new { id = entityId });
                return rv3.HasValue ? (Visibility)rv3.Value : Visibility.Private;

            case nameof(ProjectSpace):
                var sqlSpace =
                    @"SELECT COALESCE(ps.""Visibility"", pw.""Visibility"") as Visibility
                    FROM ""ProjectSpaces"" ps
                    INNER JOIN ""ProjectWorkspaces"" pw ON ps.""ProjectWorkspaceId"" = pw.""Id""
                    WHERE ps.""Id"" = @id";
                var rv4 = await _dbConnection.QuerySingleOrDefaultAsync<int?>(sqlSpace, new { id = entityId });
                return rv4.HasValue ? (Visibility)rv4.Value : Visibility.Private;

            case nameof(ProjectWorkspace):
                var sqlWs = @"SELECT ""Visibility"" FROM ""ProjectWorkspaces"" WHERE ""Id"" = @id";
                var rv5 = await _dbConnection.QuerySingleOrDefaultAsync<int?>(sqlWs, new { id = entityId });
                return rv5.HasValue ? (Visibility)rv5.Value : Visibility.Private;

            default:
                throw new NotSupportedException($"GetEffectiveVisibilityForEntityAsync not implemented for {entityType.Name}");
        }
    }

    public async Task ValidateVisibilityAgainstAncestorsAsync(Type entityType, Guid entityId, Visibility candidateVisibility, CancellationToken ct = default)
    {
        switch (entityType.Name)
        {
            case nameof(ProjectList):
                var sqlParentVis =
                @"SELECT COALESCE(pf.""Visibility"", ps.""Visibility"", pw.""Visibility"") as ParentVisibility
                FROM ""ProjectLists"" pl
                LEFT JOIN ""ProjectFolders"" pf ON pl.""ProjectFolderId"" = pf.""Id""
                INNER JOIN ""ProjectSpaces"" ps ON COALESCE(pf.""ProjectSpaceId"", pl.""ProjectSpaceId"") = ps.""Id""
                INNER JOIN ""ProjectWorkspaces"" pw ON ps.""ProjectWorkspaceId"" = pw.""Id""
                WHERE pl.""Id"" = @id";
                var pVis = await _dbConnection.QuerySingleOrDefaultAsync<int?>(sqlParentVis, new { id = entityId });
                if (pVis.HasValue && candidateVisibility > (Visibility)pVis.Value)
                    throw new InvalidOperationException("Cannot make child entity more public than its parent workspace/space/folder.");
                break;
            // Add other entity cases where write-time enforcement is necessary.
            default:
                break;
        }
    }

    // ------------------------------------------------------------
    // Cache invalidation helpers
    // ------------------------------------------------------------

    public async Task InvalidateUserPermissionCacheAsync(Guid userId, Guid workspaceId)
    {
        var cacheKey = $"user_permissions_{userId}_{workspaceId}";
        await _cache.RemoveAsync(cacheKey);
    }

    public async Task InvalidateWorkspaceTraversalCacheAsync<T>(Guid entityId)
    {
        var cacheKey = $"workspace_lookup_{typeof(T).Name}_{entityId}";
        await _cache.RemoveAsync(cacheKey);
    }

    public async Task InvalidateHierarchyTraversalCacheAsync(Guid parentEntityId, Type parentEntityType)
    {
        _logger.LogInformation("Hierarchy change detected for {EntityType} {EntityId} - consider implementing pattern-based cache invalidation", parentEntityType.Name, parentEntityId);
    }

    // ------------------------------------------------------------
    // Helpers: roles -> permissions mapping
    // ------------------------------------------------------------

    private static Permission GetRolePermissions(Role role) => role switch
    {
        Role.Owner => PermissionHelper.GetOwnerPermissions(),
        Role.Admin => PermissionHelper.GetAllPermissions(),
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
            if ((GetRolePermissions(role) & permission) == permission) roles.Add(role);
        }
        return roles;
    }

    private static bool IsPermissionAllowedForPublic(Permission permission)
    {
        var publicAllowed = Permission.View_Workspace
                            | Permission.View_Spaces
                            | Permission.View_Lists
                            | Permission.View_Tasks
                            | Permission.View_Statuses
                            | Permission.View_Comments
                            | Permission.View_Attachments;
        return (publicAllowed & permission) == permission;
    }
}
