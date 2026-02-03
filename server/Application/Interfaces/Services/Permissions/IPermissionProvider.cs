using Domain.Permission;

namespace Application.Interfaces.Services.Permissions;

public interface IPermissionProvider
{
    Task<PermissionContext> GetPermissionsFor(Guid userId, Guid workspaceId, CancellationToken ct);
}