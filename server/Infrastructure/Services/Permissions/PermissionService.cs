
using Domain.Enums;
using Domain.Enums.RelationShip;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Application.Interfaces.Services.Permissions;
using Domain;
using Application.Common;
using Domain.Enums.Workspace;

namespace Infrastructure.Services.Permissions
{
    public class PermissionService : IPermissionService
    {
        private readonly TaskPlanDbContext _context;
        private readonly HybridCache _cache;
        private readonly WorkspaceContext _workspaceContext;
        private readonly PermissionContextBuilder _builder;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(TaskPlanDbContext context, HybridCache cache, PermissionContextBuilder builder, ILogger<PermissionService> logger, WorkspaceContext workspaceContext)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
            _workspaceContext = workspaceContext;
            _builder = builder;
        }

        public async Task<bool> HasPermissionAsync(Guid userId, Guid? entityId, EntityType entityType, PermissionAction action, CancellationToken cancellationToken = default)
        {
            var workspaceId = _workspaceContext.WorkspaceId;
            var context = await _builder.BuildMinimalAsync(userId, workspaceId, entityId, entityType, cancellationToken);
            var result = PermissionMatrix.CanPerform(entityType, action, context);

            if (!result)
            {
                _logger.LogWarning(
                    "Permission denied: User {UserId} attempted {Action} on {EntityType} {EntityId}",
                    userId, action, entityType, entityId);
            }

            return result;
        }


    }
}
