using System;
using Application.Common.Results;
using Domain.Enums;

namespace Application.Interfaces.Services.Permissions;

public interface IWritePermissionService
{
    Task<WritePermissionResult> CheckWritePermissionAsync(Guid userId, EntityType entityType, Guid entityId, Permission requiredPermission, Guid workspaceId, CancellationToken ct = default)
}
