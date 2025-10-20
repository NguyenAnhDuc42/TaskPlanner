using System;
using Domain.Enums;

namespace Company.ClassLibrary1;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(Guid userId, Guid workspaceId, Guid? entityId, EntityType entityType, Permission requiredPermission, CancellationToken cancellationToken = default);
}
