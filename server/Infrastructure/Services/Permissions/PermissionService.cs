
using Domain.Enums;
using Domain.Enums.RelationShip;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Application.Interfaces.Services.Permissions;
using Domain;

namespace Infrastructure.Services.Permissions
{
    public class PermissionService : IPermissionService
    {
        private readonly TaskPlanDbContext _context;
        private readonly HybridCache _cache;
        private readonly WorkspaceContext _workspaceContext;
        private readonly ILogger<PermissionService> _logger;

        private const string EntityMemberPermissionKey = "entity_member_{0}_{1}_{2}";
        private const string WorkspaceMemberKey = "workspace_member_{0}_{1}";

        public PermissionService(
            TaskPlanDbContext context,
            HybridCache cache,
            ILogger<PermissionService> logger,
            WorkspaceContext workspaceContext)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
            _workspaceContext = workspaceContext;
        }

        public async Task<bool> HasPermissionAsync(
            Guid userId,
            Guid? entityId,
            EntityType entityType,
            PermissionAction action,
            CancellationToken cancellationToken = default)
        {
            // --- 1. Workspace-level checks ---
            var workspaceId = _workspaceContext.WorkspaceId;
            var workspaceRole = await GetWorkspaceRoleAsync(userId, workspaceId, cancellationToken);

            // Special workspace-level action rules (delete workspace / manage members)
            if (entityType == EntityType.ProjectWorkspace)
            {
                var ws = await _context.ProjectWorkspaces.AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);

                if (ws == null) return false;

                // Delete workspace: only owner or creator
                if (action == PermissionAction.Delete)
                    return ws.CreatorId == userId || workspaceRole == Role.Owner;

                // Other workspace-scoped actions use matrix
                return PermissionMatrix.Can(workspaceRole, action);
            }

            // --- 2. Entity-level override ---
            if (entityId.HasValue)
            {
                var accessLevel = await GetEntityAccessLevelAsync(userId, entityId.Value, entityType, cancellationToken);
                if (accessLevel.HasValue)
                {
                    var isCreator = await IsEntityCreatorAsync(userId, entityId.Value, entityType, cancellationToken);
                    return PermissionMatrix.Can(accessLevel.Value, action, isCreator);
                }
            }

            // --- 3. Fallback to workspace role ---
            return PermissionMatrix.Can(workspaceRole, action);
        }

        private async Task<Role> GetWorkspaceRoleAsync(Guid userId, Guid workspaceId, CancellationToken ct)
        {
            var cacheKey = string.Format(WorkspaceMemberKey, userId, workspaceId);

            return await _cache.GetOrCreateAsync(cacheKey, async factory =>
            {
                var wm = await _context.WorkspaceMembers.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.ProjectWorkspaceId == workspaceId && x.UserId == userId, ct);

                return wm?.Role ?? Role.Guest;
            }, new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) });
        }

        private async Task<AccessLevel?> GetEntityAccessLevelAsync(Guid userId, Guid entityId, EntityType entityType, CancellationToken ct)
        {
            var cacheKey = string.Format(EntityMemberPermissionKey, userId, entityId, entityType);

            return await _cache.GetOrCreateAsync(cacheKey, async factory =>
            {
                var em = await _context.EntityMembers.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.EntityId == entityId && x.EntityType.ToString() == entityType.ToString() && x.UserId == userId, ct);

                return em?.AccessLevel;
            }, new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) });
        }

        private async Task<bool> IsEntityCreatorAsync(Guid userId, Guid entityId, EntityType entityType, CancellationToken ct)
        {
            return entityType switch
            {
                EntityType.ProjectSpace => await _context.ProjectSpaces.AsNoTracking().AnyAsync(x => x.Id == entityId && x.CreatorId == userId, ct),
                EntityType.ProjectFolder => await _context.ProjectFolders.AsNoTracking().AnyAsync(x => x.Id == entityId && x.CreatorId == userId, ct),
                EntityType.ProjectList => await _context.ProjectLists.AsNoTracking().AnyAsync(x => x.Id == entityId && x.CreatorId == userId, ct),
                EntityType.ProjectTask => await _context.ProjectTasks.AsNoTracking().AnyAsync(x => x.Id == entityId && x.CreatorId == userId, ct),
                _ => false
            };
        }
    }
}
