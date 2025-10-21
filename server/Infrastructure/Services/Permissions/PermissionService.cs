using System;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Permissions;

public class PermissionService : IPermissionService
{
    private readonly TaskPlanDbContext _context;
    private readonly HybridCache _cache;
    private readonly WorkspaceContext _workspaceContext;
    private readonly ILogger<PermissionService> _logger;
    private const string EntityMemberPermissionKey = "entity_member_{0}_{1}_{2}";
    private const string WorkspaceMemberKey = "workspace_member_{0}_{1}";
    public PermissionService(TaskPlanDbContext context, HybridCache cache, ILogger<PermissionService> logger, WorkspaceContext workspaceContext)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
        _workspaceContext = workspaceContext;
    }

    public async Task<bool> HasPermissionAsync(Guid userId, Guid? entityId, EntityType entityType, Permission requiredPermission, CancellationToken cancellationToken = default)
    {
        var entityName = entityType.ToString();
        var workspaceId = _workspaceContext.WorkspaceId;
        if (entityType == EntityType.ProjectWorkspace)
        {
            var rolePermissions = await GetWorkspaceMemberRolePermissionsAsync(userId, cancellationToken);
            return (rolePermissions & requiredPermission) == requiredPermission;
        }
        if (entityId.HasValue)
        {
            var entityPermission = await GetEntityMemberPermissionAsync(userId, entityId.Value, entityName, cancellationToken);

            if (entityPermission != Permission.None)
            {
                return (entityPermission & requiredPermission) == requiredPermission;
            }
        }
        var rolePermissionsFromFallback = await GetWorkspaceMemberRolePermissionsAsync(userId, cancellationToken);
        return (rolePermissionsFromFallback & requiredPermission) == requiredPermission;
    }
    private async Task<Permission> GetWorkspaceMemberRolePermissionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var workspaceId = _workspaceContext.WorkspaceId;
        var cacheKey = string.Format(WorkspaceMemberKey, userId, workspaceId);

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async factory =>
            {
                var workspaceMember = await _context.WorkspaceMembers.FirstOrDefaultAsync(wm => wm.ProjectWorkspaceId == workspaceId && wm.UserId == userId, cancellationToken);

                if (workspaceMember == null)
                    return Permission.None;

                return GetRolePermissions(workspaceMember.Role);
            },
             options: new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) },
            cancellationToken: cancellationToken);
    }
    private async Task<Permission> GetEntityMemberPermissionAsync(Guid userId, Guid entityId, string entityType, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(EntityMemberPermissionKey, userId, entityId, entityType);

        return await _cache.GetOrCreateAsync(cacheKey, async factort =>
        {
            var entityMember = await _context.EntityMembers.FirstOrDefaultAsync(em => em.EntityId == entityId && em.EntityType.ToString() == entityType && em.UserId == userId, cancellationToken);
            if (entityMember == null) return Permission.None;
            return ConvertAccessLevelToPermission(entityMember.AccessLevel);
        },

        options: new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) },
        cancellationToken: cancellationToken
        );
    }

    private Permission GetRolePermissions(Role role) => role switch
    {
        Role.Owner => Permission.Owner_Permissions,
        Role.Admin => Permission.Workspace_Admin | Permission.Member_Admin | Permission.Content_Admin | Permission.View_Reports | Permission.Export_Data,
        Role.Member => Permission.View_Workspace | Permission.View_Members | Permission.View_Spaces | Permission.Create_Spaces | Permission.View_Lists | Permission.Create_Lists | Permission.View_Tasks | Permission.Create_Tasks | Permission.View_Comments | Permission.Create_Comments | Permission.Edit_Own_Comments | Permission.View_Attachments | Permission.Upload_Attachments | Permission.Delete_Own_Attachments,
        Role.Guest => Permission.View_Workspace | Permission.View_Spaces | Permission.View_Lists | Permission.View_Tasks | Permission.View_Comments | Permission.View_Attachments | Permission.View_Reports,
        _ => Permission.None
    };

    private Permission ConvertAccessLevelToPermission(AccessLevel accessLevel) => accessLevel switch
    {
        AccessLevel.Viewer => GetReadPermissions(),
        AccessLevel.Editor => GetReadPermissions() | GetWritePermissions(),
        AccessLevel.Manager => Permission.All,
        _ => Permission.None
    };

    private Permission GetReadPermissions() =>
        Permission.View_Workspace | Permission.View_Members | Permission.View_Spaces |
        Permission.View_Lists | Permission.View_Tasks | Permission.View_Comments |
        Permission.View_Attachments | Permission.View_Statuses | Permission.View_Reports;

    private Permission GetWritePermissions() =>
        Permission.Edit_Workspace | Permission.Create_Spaces | Permission.Edit_Spaces |
        Permission.Create_Lists | Permission.Edit_Lists | Permission.Create_Tasks |
        Permission.Edit_Tasks | Permission.Create_Comments | Permission.Edit_Own_Comments |
        Permission.Upload_Attachments;
}
