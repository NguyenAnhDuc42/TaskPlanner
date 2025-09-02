using System;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Common.Interfaces;
using Domain.Entities.Relationship;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Errors.Model;

namespace Infrastructure.Services;

public class PermissionService : IPermissionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly HybridCache _cache;
    private readonly ILogger<PermissionService> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public PermissionService(IUnitOfWork unitOfWork, HybridCache cache, ILogger<PermissionService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<T> GetEntityWithPermissionAsync<T>(Guid entityId, Guid userId, Permission requiredPermission, Func<IQueryable<T>, IQueryable<T>>? includeFunc = null, CancellationToken ct = default) where T : class, IHasWorkspaceId
    {
        var query = _unitOfWork.Set<T>().AsQueryable();

        if (includeFunc != null) query = includeFunc(query);
        var entity = await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == entityId, ct)
            ?? throw new NotFoundException($"{typeof(T).Name} {entityId} not found");


        await EnsurePermissionAsync(userId, entity.ProjectWorkspaceId, requiredPermission, ct);

        return entity;

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

    // --- Permissions retrieval with caching ---
    public async Task<Permission> GetUserPermissionsAsync(Guid userId, Guid workspaceId, CancellationToken ct = default)
    {
        var cacheKey = $"user_permissions_{userId}_{workspaceId}";

        // Try get or create in one step
        return await _cache.GetOrCreateAsync(cacheKey, async token =>
        {
            var membership = await _unitOfWork.Set<UserProjectWorkspace>().AsQueryable()
                .AsNoTracking()
                .Include(m => m.ProjectWorkspace)
                .FirstOrDefaultAsync(m => m.UserId == userId && m.ProjectWorkspaceId == workspaceId, token);

            Permission permissions;
            if (membership == null)
                permissions = Permission.None;
            else if (membership.ProjectWorkspace.CreatorId == userId)
                permissions = Permission.All;
            else
                permissions = GetRolePermissions(membership.Role);

            return permissions;

        }, new HybridCacheEntryOptions { Expiration = CacheDuration });
    }

    public async Task<IEnumerable<Guid>> GetUserAccessibleWorkspacesAsync(Guid userId, Permission permission, CancellationToken ct = default)
    {
        var memberships = await _unitOfWork.Set<UserProjectWorkspace>()
            .AsQueryable()
            .AsNoTracking()
            .Include(m => m.ProjectWorkspace)
            .Where(m => m.UserId == userId)
            .ToListAsync(ct);

        return memberships
            .Where(m => (m.ProjectWorkspace.CreatorId == userId ? Permission.All : GetRolePermissions(m.Role) & permission) == permission)
            .Select(m => m.ProjectWorkspaceId)
            .ToList();
    }

    public async Task<bool> IsWorkspaceOwnerAsync(Guid userId, Guid workspaceId, CancellationToken ct = default)
    {
        var membership = await _unitOfWork.Set<UserProjectWorkspace>()
            .AsQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.ProjectWorkspaceId == workspaceId, ct);

        return membership?.Role == Domain.Enums.Role.Owner;
    }

    public async Task<bool> IsWorkspaceCreatorAsync(Guid userId, Guid workspaceId, CancellationToken ct = default)
    {
        var workspace = await _unitOfWork.ProjectWorkspaces
            .Query
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workspaceId, ct);

        return workspace?.CreatorId == userId;
    }

    public async Task<Role?> GetUserRoleAsync(Guid userId, Guid workspaceId, CancellationToken ct = default)
    {
        var membership = await _unitOfWork.Set<UserProjectWorkspace>()
            .AsQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.ProjectWorkspaceId == workspaceId, ct);

        return membership?.Role;
    }

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
}
