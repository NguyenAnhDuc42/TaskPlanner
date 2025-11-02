using Domain.Common;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Application.Interfaces.Services.Permissions;
using Application.Common;
using Domain.Enums.RelationShip;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Entities.ProjectEntities;
using Domain.Enums.Workspace;
using Domain.Entities.Support.Workspace;

namespace Infrastructure.Services.Permissions
{
    public class PermissionService : IPermissionService
    {
        private readonly TaskPlanDbContext _dbContext;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(TaskPlanDbContext dbContext, ILogger<PermissionService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;

        }

        public async Task<bool> CanPerformAsync<TEntity>(Guid workspaceId, Guid userId, TEntity entity, PermissionAction action, CancellationToken ct) where TEntity : Entity
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            ct.ThrowIfCancellationRequested();
            var (role, access) = await GetLayeredPermissionsAsync(userId, workspaceId, entity, ct);
            var isCreator = ExtractCreator(entity, userId);
            var (isArchived, isPrivate) = ExtractEntityState(entity);
            var entityType = GetEntityType<TEntity>();
            var context = new PermissionContext
            {
                UserId = userId,
                WorkspaceId = workspaceId,
                EntityId = entity.Id,
                EntityType = entityType,
                WorkspaceRole = role ?? Role.Guest,
                EntityAccess = access,
                IsCreator = isCreator,
                IsEntityArchived = isArchived,
                IsEntityPrivate = isPrivate
            };
            var result = PermissionMatrix.CanPerform(entityType, action, context);
            if (!result) _logger.LogWarning("Permission denied: User {UserId} action {Action} on {EntityType} {EntityId}", userId, action, entityType, entity.Id);
            return result;
        }

        public async Task<bool> CanPerformAsync<TParent>(Guid workspaceId, Guid userId, TParent parentEntity, EntityType childType, PermissionAction action, CancellationToken ct)
            where TParent : Entity
        {
            if (parentEntity == null) throw new ArgumentNullException(nameof(parentEntity));
            ct.ThrowIfCancellationRequested();
            var (role, access) = await GetLayeredPermissionsAsync(userId, workspaceId, parentEntity, ct);
            var parentType = GetEntityType<TParent>();

            var context = new PermissionContext
            {
                UserId = userId,
                WorkspaceId = workspaceId,
                EntityId = parentEntity.Id,
                EntityType = parentType,
                WorkspaceRole = role ?? Role.Guest,
                EntityAccess = access,
                IsEntityPrivate = ExtractEntityState(parentEntity).IsPrivate
                // Don't set IsCreator, IsArchived for parent - checking ability to create IN it
            };

            return PermissionMatrix.CanPerform(parentType, action, context);

        }

        public async Task<bool> CanPerformAsync<TChild, TParent>(Guid workspaceId, Guid userId, TChild childEntity, TParent parentEntity, PermissionAction action, CancellationToken ct)
            where TChild : Entity
            where TParent : Entity
        {
            if (parentEntity == null) throw new ArgumentNullException(nameof(parentEntity));
            if (childEntity == null) throw new ArgumentNullException(nameof(childEntity));
            ct.ThrowIfCancellationRequested();
            var childType = GetEntityType<TChild>();
            var (role, access) = await GetLayeredPermissionsAsync(userId, workspaceId, childEntity, ct);
            var isCreator = ExtractCreator(childEntity, userId);
            var (isArchived, isPrivate) = ExtractEntityState(childEntity);

            // If parent is ChatRoom, get chat room specific data
            ChatRoomRole? chatRole = null;
            bool isBanned = false, isMuted = false;

            if (parentEntity is ChatRoom cr)
            {
                chatRole = await GetChatRoomRoleAsync(userId, cr.Id, ct);
                var status = await GetChatRoomMemberStatusAsync(userId, cr.Id, ct);
                isBanned = status.IsBanned;
                isMuted = status.IsMuted;
            }

            var context = new PermissionContext
            {
                UserId = userId,
                WorkspaceId = workspaceId,
                EntityId = childEntity.Id,
                EntityType = childType,
                WorkspaceRole = role ?? Role.Guest,
                EntityAccess = access,
                ChatRoomRole = chatRole,
                IsUserBannedFromChatRoom = isBanned,
                IsUserMutedInChatRoom = isMuted,
                IsCreator = isCreator,
                IsEntityArchived = isArchived,
                IsEntityPrivate = isPrivate
            };

            return PermissionMatrix.CanPerform(childType, action, context);
        }

        private async Task<(Role?, AccessLevel?)> GetLayeredPermissionsAsync<TEntity>(Guid userId, Guid workspaceId, TEntity entity, CancellationToken ct) where TEntity : Entity
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            ct.ThrowIfCancellationRequested();

            switch (entity)
            {
                case ProjectWorkspace:
                    var wsRole = await GetWorkspaceRoleAsync(userId, workspaceId, ct);
                    return (wsRole, null);

                case ProjectSpace ps:
                    var spaceAccess = await GetEntityAccessLevelAsync(userId, ps.Id, EntityType.ProjectSpace, ct);
                    if (spaceAccess.HasValue) return (null, spaceAccess);
                    var spaceRole = await GetWorkspaceRoleAsync(userId, workspaceId, ct);
                    return (spaceRole, null);

                case ProjectFolder pf:
                    var folderAccess = await GetEntityAccessLevelAsync(userId, pf.Id, EntityType.ProjectFolder, ct);
                    if (folderAccess.HasValue) return (null, folderAccess);
                    var folderRole = await GetWorkspaceRoleAsync(userId, workspaceId, ct);
                    return (folderRole, null);

                case ProjectList pl:
                    var listAccess = await GetEntityAccessLevelAsync(userId, pl.Id, EntityType.ProjectList, ct);
                    if (listAccess.HasValue) return (null, listAccess);
                    var listRole = await GetWorkspaceRoleAsync(userId, workspaceId, ct);
                    return (listRole, null);

                default:
                    var defaultRole = await GetWorkspaceRoleAsync(userId, workspaceId, ct);
                    return (defaultRole, null);
            }
        }
        private async Task<Role> GetWorkspaceRoleAsync(Guid userId, Guid workspaceId, CancellationToken ct)
        {
            var result = await _dbContext.WorkspaceMembers
                .Where(wm => wm.ProjectWorkspaceId == workspaceId && wm.UserId == userId)
                .Select(wm => wm.Role) // directly project to nullable Role
                .FirstOrDefaultAsync(ct);
            return result; // null if no membership found
        }
        private async Task<AccessLevel?> GetEntityAccessLevelAsync(Guid userId, Guid entityId, EntityType entityType, CancellationToken ct)
        {
            var result = await _dbContext.EntityMembers
                .Where(em => em.EntityId == entityId && em.UserId == userId && em.EntityType.ToString() == entityType.ToString())
                .Select(em => (AccessLevel?)em.AccessLevel) // directly project to nullable AccessLevel
                .FirstOrDefaultAsync(ct);

            return result; // null if no membership found
        }

        private async Task<ChatRoomRole?> GetChatRoomRoleAsync(Guid userId, Guid chatRoomId, CancellationToken ct)
        {
            return await _dbContext.ChatRoomMembers
                .Where(crm => crm.ChatRoomId == chatRoomId && crm.UserId == userId)
                .Select(crm => (ChatRoomRole?)crm.Role)
                .FirstOrDefaultAsync(ct);
        }

        private async Task<(bool IsBanned, bool IsMuted)> GetChatRoomMemberStatusAsync(Guid userId, Guid chatRoomId, CancellationToken ct)
        {
            var member = await _dbContext.ChatRoomMembers
                .Where(crm => crm.ChatRoomId == chatRoomId && crm.UserId == userId)
                .Select(crm => new { crm.IsBanned, crm.IsMuted, crm.MuteEndTime })
                .FirstOrDefaultAsync(ct);

            if (member == null) return (false, false);

            var isMuted = member.IsMuted && (member.MuteEndTime == null || member.MuteEndTime > DateTimeOffset.UtcNow);
            return (member.IsBanned, isMuted);
        }
        private static bool ExtractCreator<TEntity>(TEntity entity, Guid userId) where TEntity : Entity =>
            entity switch
            {
                ProjectWorkspace pw => pw.CreatorId == userId,
                ProjectSpace ps => ps.CreatorId == userId,
                ProjectFolder pf => pf.CreatorId == userId,
                ProjectList pl => pl.CreatorId == userId,
                _ => false
            };

        private static (bool IsArchived, bool IsPrivate) ExtractEntityState<TEntity>(TEntity entity) where TEntity : Entity =>
        entity switch
        {
            ProjectSpace ps => (ps.IsArchived, ps.IsPrivate),
            ProjectFolder pf => (pf.IsArchived, pf.IsPrivate),
            ProjectList pl => (pl.IsArchived, pl.IsPrivate),
            _ => (false, false)
        };

        private static EntityType GetEntityType<TEntity>() where TEntity : Entity =>
        typeof(TEntity).Name switch
        {
            nameof(ProjectWorkspace) => EntityType.ProjectWorkspace,
            nameof(ProjectSpace) => EntityType.ProjectSpace,
            nameof(ProjectFolder) => EntityType.ProjectFolder,
            nameof(ProjectList) => EntityType.ProjectList,
            _ => throw new InvalidOperationException($"Unknown entity type: {typeof(TEntity).Name}")
        };



    }
}