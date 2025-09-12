using System;
using Domain.Common;
using Domain.Common.Interfaces;
using Domain.Enums;

namespace Application.Interfaces.Services;

public interface IPermissionService
{
    Task<T> GetEntityWithPermissionAsync<T>(Guid entityId, Guid userId, Permission requiredPermission, Func<IQueryable<T>, IQueryable<T>>? includeFunc = null, CancellationToken ct = default) where T : class;
    Task<bool> HasPermissionAsync(Guid userId, Guid workspaceId, Permission permission, CancellationToken ct = default);
    Task<bool> HasPermissionAsync(Guid userId, Guid workspaceId, Permission[] permissions, CancellationToken ct = default);
    Task EnsurePermissionAsync(Guid userId, Guid workspaceId, Permission permission, CancellationToken ct = default);
    Task EnsurePermissionAsync(Guid userId, Guid workspaceId, Permission[] permissions, CancellationToken ct = default);
    Task<Permission> GetUserPermissionsAsync(Guid userId, Guid workspaceId, CancellationToken ct = default);
    Task<IEnumerable<Guid>> GetUserAccessibleWorkspacesAsync(Guid userId, Permission permission, CancellationToken ct = default);
    Task<bool> IsWorkspaceOwnerAsync(Guid userId, Guid workspaceId, CancellationToken ct = default);
    Task<bool> IsWorkspaceCreatorAsync(Guid userId, Guid workspaceId, CancellationToken ct = default);
    Task<Role?> GetUserRoleAsync(Guid userId, Guid workspaceId, CancellationToken ct = default);
}
