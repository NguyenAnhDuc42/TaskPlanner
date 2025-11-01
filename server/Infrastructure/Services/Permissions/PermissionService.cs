using Domain.Common;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Application.Interfaces.Services.Permissions;
using Application.Common;
using Domain.Enums.RelationShip;
using Infrastructure.Data;

namespace Infrastructure.Services.Permissions
{
    public class PermissionService : IPermissionService
    {
        private readonly TaskPlanDbContext _dbContext;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(TaskPlanDbContext dbContext,ILogger<PermissionService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;

        }

        public Task<bool> CanPerformAsync<TEntity>(Guid workspaceId, Guid userId, TEntity entity, PermissionAction action, CancellationToken ct) where TEntity : Entity
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            ct.ThrowIfCancellationRequested();
            var context = new PermissionContext
            {
                UserId = userId,
                WorkspaceId = workspaceId
            };
        }

        public Task<bool> CanPerformAsync<TParent>(Guid workspaceId, Guid userId, TParent parentEntity, EntityType childType, PermissionAction action, CancellationToken ct)
            where TParent : Entity
        {
            if (parentEntity == null) throw new ArgumentNullException(nameof(parentEntity));
            ct.ThrowIfCancellationRequested();
            var context = new PermissionContext
            {
                UserId = userId,
                WorkspaceId = workspaceId
            };
            throw new NotImplementedException();
        }

        public Task<bool> CanPerformAsync<TChild, TParent>(Guid workspaceId, Guid userId, TChild childEntity, TParent parentEntity, PermissionAction action, CancellationToken ct)
            where TChild : Entity
            where TParent : Entity
        {
            if (parentEntity == null) throw new ArgumentNullException(nameof(parentEntity));
            if (childEntity == null) throw new ArgumentNullException(nameof(childEntity));
            ct.ThrowIfCancellationRequested();
            var context = new PermissionContext
            {
                UserId = userId,
                WorkspaceId = workspaceId
            };
            throw new NotImplementedException();
        }

        private async Task<(Role? role, AccessLevel? access)> GetLayeredPermissionsAsync<TEntity>(TEntity entity, CancellationToken ct) where TEntity : Entity
        {
            async Task<Role> GetWorkspaceRoleAsync(Guid userId, Guid workspaceId, CancellationToken ct)
            {
                var role = await _dbContext.WorkspaceMembers
            }
            switch (entity)
            {

            }
        }

    }
}