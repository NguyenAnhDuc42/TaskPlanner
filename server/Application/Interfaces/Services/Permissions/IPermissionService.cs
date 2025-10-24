using System;
using Domain.Enums;

namespace Application.Interfaces.Services.Permissions;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(Guid userId, Guid? entityId, EntityType entityType, PermissionAction action, CancellationToken cancellationToken = default);
}
