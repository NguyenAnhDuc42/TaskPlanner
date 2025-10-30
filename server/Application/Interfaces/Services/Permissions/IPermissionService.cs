using System;
using Domain.Common.Interfaces;
using Domain.Enums;

namespace Application.Interfaces.Services.Permissions;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync<TEntity>(Guid userId, TEntity entity, PermissionAction action, CancellationToken ct = default) where TEntity : IIdentifiable;
    Task<bool> CanPerformInScopeAsync(Guid userId, Guid? scopeId, EntityType scopeType, PermissionAction action, CancellationToken cancellationToken = default);
}
