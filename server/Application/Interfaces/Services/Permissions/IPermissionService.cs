using System;
using Domain.Common.Interfaces;
using Domain.Enums;

namespace Application.Interfaces.Services.Permissions;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync<TEntity>(Guid userId, TEntity entity, PermissionAction action, CancellationToken ct) where TEntity : IIdentifiable;
    // Task<bool> HasPermissionAsync(Guid userId, Guid? entityId, EntityType entityType, PermissionAction action, CancellationToken cancellationToken = default);
}
