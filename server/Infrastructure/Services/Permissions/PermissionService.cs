
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
        private readonly ILogger<PermissionService> _logger;
        private const string WorkspaceMemberKey = "workspace_member_{0}_{1}";
        private const string EntityMemberPermissionKey = "entity_member_{0}_{1}_{2}";
        private const string ChatRoomMemberKey = "chat_room_member_{0}_{1}";
        private const string EntityCreatorKey = "entity_creator_{0}_{1}_{2}";

        public PermissionService(TaskPlanDbContext context, HybridCache cache, ILogger<PermissionService> logger, WorkspaceContext workspaceContext)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
            _workspaceContext = workspaceContext;
        }

        public async Task<bool> HasPermissionAsync(Guid userId, Guid? entityId, EntityType entityType, PermissionAction action, CancellationToken cancellationToken = default)
        {
            var workspaceId = _workspaceContext.WorkspaceId;

            // Build permission context - this is what the matrix rules will evaluate
            var context = await BuildPermissionContextAsync(userId, workspaceId, entityId, entityType, cancellationToken);

            // Use matrix to check if action is allowed
            var result = PermissionMatrix.CanPerform(entityType, action, context);

            if (!result)
            {
                _logger.LogWarning(
                    "Permission denied: User {UserId} attempted {Action} on {EntityType} {EntityId}",
                    userId, action, entityType, entityId);
            }

            return result;
        }
        private async Task<PermissionContext> BuildPermissionContextAsync(
            Guid userId,
            Guid workspaceId,
            Guid? entityId,
            EntityType entityType,
            CancellationToken ct)
        {
            var workspaceRole = await GetWorkspaceRoleAsync(userId, workspaceId, ct);
            var isWorkspaceOwner = workspaceRole == Role.Owner;
            var isWorkspaceAdmin = workspaceRole == Role.Admin;

            var entityAccess = entityId.HasValue
                ? await GetEntityAccessLevelAsync(userId, entityId.Value, entityType, ct)
                : null;

            var chatRoomRole = entityType == EntityType.ChatRoom && entityId.HasValue
                ? await GetChatRoomRoleAsync(userId, entityId.Value, ct)
                : null;

            var isCreator = entityId.HasValue
                ? await IsEntityCreatorAsync(userId, entityId.Value, entityType, ct)
                : false;

            // Fetch entity state (archived, private, created at, etc)
            var (isArchived, isPrivate, createdAt) = entityId.HasValue
                ? await GetEntityStateAsync(entityId.Value, entityType, ct)
                : (false, false, null);

            // Get active child count for archive checks
            var activeChildCount = entityId.HasValue
                ? await GetActiveChildCountAsync(entityId.Value, entityType, ct)
                : null;

            // Message age in minutes
            var messageAgeMinutes = entityType == EntityType.ChatMessage && entityId.HasValue
                ? await GetMessageAgeMinutesAsync(entityId.Value, ct)
                : 0;

            // Chat room specific checks
            var (isBanned, isMuted) = entityType == EntityType.ChatRoom && entityId.HasValue
                ? await GetChatRoomMemberStatusAsync(userId, entityId.Value, ct)
                : (false, false);

            return new PermissionContext
            {
                UserId = userId,
                WorkspaceId = workspaceId,
                EntityId = entityId,
                EntityType = entityType,
                WorkspaceRole = workspaceRole,
                IsWorkspaceOwner = isWorkspaceOwner,
                IsWorkspaceAdmin = isWorkspaceAdmin,
                IsUserSuspendedInWorkspace = await IsUserSuspendedInWorkspaceAsync(userId, workspaceId, ct),
                EntityAccess = entityAccess,
                IsEntityManager = entityAccess == AccessLevel.Manager,
                IsEntityEditor = entityAccess == AccessLevel.Editor,
                IsEntityViewer = entityAccess == AccessLevel.Viewer,
                ChatRoomRole = chatRoomRole,
                IsChatRoomOwner = chatRoomRole == ChatRoomRole.Owner,
                IsUserBannedFromChatRoom = isBanned,
                IsUserMutedInChatRoom = isMuted,
                IsCreator = isCreator,
                IsEntityArchived = isArchived,
                IsEntityPrivate = isPrivate,
                EntityCreatedAt = createdAt,
                ActiveChildCount = activeChildCount,
                HasParentEntity = entityId.HasValue && await HasParentAsync(entityId.Value, entityType, ct),
                ParentEntityId = entityId.HasValue ? await GetParentIdAsync(entityId.Value, entityType, ct) : null,
                MessageAgeMinutes = messageAgeMinutes,
                IsMessagePinned = entityType == EntityType.ChatMessage && entityId.HasValue ? await IsMessagePinnedAsync(entityId.Value, ct) : false
            };
        }
        private async Task<Role> GetWorkspaceRoleAsync(Guid userId, Guid workspaceId, CancellationToken ct)
        {
            var cacheKey = string.Format(WorkspaceMemberKey, userId, workspaceId);

            return await _cache.GetOrCreateAsync(cacheKey, async factory =>
            {
                var wm = await _context.WorkspaceMembers.AsNoTracking().FirstOrDefaultAsync(x => x.ProjectWorkspaceId == workspaceId && x.UserId == userId, ct);
                return wm?.Role ?? Role.Guest;
            }, new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) });
        }

        private async Task<AccessLevel?> GetEntityAccessLevelAsync(Guid userId, Guid entityId, EntityType entityType, CancellationToken ct)
        {
            var cacheKey = string.Format(EntityMemberPermissionKey, userId, entityId, entityType);

            return await _cache.GetOrCreateAsync(cacheKey, async factory =>
            {
                var em = await _context.EntityMembers.AsNoTracking().FirstOrDefaultAsync(x => x.EntityId == entityId && x.EntityType.ToString() == entityType.ToString() && x.UserId == userId, ct);
                return em?.AccessLevel;
            }, new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) });
        }
        private async Task<ChatRoomRole?> GetChatRoomRoleAsync(Guid userId, Guid chatRoomId, CancellationToken ct)
        {
            var cacheKey = string.Format(ChatRoomMemberKey, userId, chatRoomId);
            return await _cache.GetOrCreateAsync(cacheKey, async factory =>
            {
                var crm = await _context.ChatRoomMembers.AsNoTracking().FirstOrDefaultAsync(x => x.ChatRoomId == chatRoomId && x.UserId == userId, ct);
                return crm?.Role;
            }, new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) });
        }

        private async Task<bool> IsUserSuspendedInWorkspaceAsync(Guid userId, Guid workspaceId, CancellationToken ct)
        {
            return await _context.WorkspaceMembers
                .AsNoTracking()
                .AnyAsync(wm => wm.ProjectWorkspaceId == workspaceId && wm.UserId == userId && wm.Status == MembershipStatus.Suspended, ct);
        }


        private async Task<(bool IsArchived, bool IsPrivate, DateTimeOffset? CreatedAt)> GetEntityStateAsync(Guid entityId, EntityType entityType, CancellationToken ct)
        {
            // 1. Define the default state tuple
            var defaultState = (false, false, (DateTimeOffset?)null);

            // Local function to handle the ProjectTask query and mapping
            async Task<(bool IsArchived, bool IsPrivate, DateTimeOffset? CreatedAt)> GetTaskState()
            {
                // Query returns an anonymous type
                var result = await _context.ProjectTasks
                    .AsNoTracking()
                    .Where(t => t.Id == entityId)
                    .Select(t => new
                    {
                        t.IsArchived,
                        IsPrivate = false,
                        CreatedAt = (DateTimeOffset?)t.CreatedAt
                    })
                    .FirstOrDefaultAsync(ct);

                // Explicitly map the anonymous type to the required named tuple
                return result != null
                    ? (result.IsArchived, result.IsPrivate, result.CreatedAt)
                    : defaultState;
            }

            // Local function for ProjectList
            async Task<(bool IsArchived, bool IsPrivate, DateTimeOffset? CreatedAt)> GetListState()
            {
                var result = await _context.ProjectLists
                    .AsNoTracking()
                    .Where(l => l.Id == entityId)
                    .Select(l => new
                    {
                        l.IsArchived,
                        IsPrivate = false,
                        CreatedAt = (DateTimeOffset?)l.CreatedAt
                    })
                    .FirstOrDefaultAsync(ct);

                return result != null
                    ? (result.IsArchived, result.IsPrivate, result.CreatedAt)
                    : defaultState;
            }

            // Local function for ChatRoom
            async Task<(bool IsArchived, bool IsPrivate, DateTimeOffset? CreatedAt)> GetChatRoomState()
            {
                var result = await _context.ChatRooms
                    .AsNoTracking()
                    .Where(cr => cr.Id == entityId)
                    .Select(cr => new
                    {
                        cr.IsArchived,
                        cr.IsPrivate,
                        CreatedAt = (DateTimeOffset?)cr.CreatedAt
                    })
                    .FirstOrDefaultAsync(ct);

                return result != null
                    ? (result.IsArchived, result.IsPrivate, result.CreatedAt)
                    : defaultState;
            }


            // Use the local functions in the switch expression
            return entityType switch
            {
                EntityType.ProjectTask => await GetTaskState(),
                EntityType.ProjectList => await GetListState(),
                EntityType.ChatRoom => await GetChatRoomState(),
                // The default case returns the explicit tuple type
                _ => defaultState
            };
        }
        private async Task<int?> GetActiveChildCountAsync(Guid entityId, EntityType entityType, CancellationToken ct)
        {
            return entityType switch
            {
                EntityType.ProjectList => await _context.ProjectTasks
                    .AsNoTracking()
                    .CountAsync(t => t.ProjectListId == entityId && !t.IsArchived, ct),
                EntityType.ProjectFolder => await _context.ProjectLists
                    .AsNoTracking()
                    .CountAsync(l => l.ProjectFolderId == entityId && !l.IsArchived, ct),
                _ => await Task.FromResult(0)
            };
        }
        private async Task<int> GetMessageAgeMinutesAsync(Guid messageId, CancellationToken ct)
        {
            var message = await _context.ChatMessages
                .AsNoTracking()
                .Where(m => m.Id == messageId)
                .Select(m => m.CreatedAt)
                .FirstOrDefaultAsync(ct);

            return (int)(DateTimeOffset.UtcNow - message).TotalMinutes;
        }
        private async Task<(bool IsBanned, bool IsMuted)> GetChatRoomMemberStatusAsync(Guid userId, Guid chatRoomId, CancellationToken ct)
        {
            var member = await _context.ChatRoomMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(crm => crm.UserId == userId && crm.ChatRoomId == chatRoomId, ct);

            if (member == null) return (false, false);

            var isBanned = member.IsBanned;
            var isMuted = member.IsMuted && (member.MuteEndTime == null || member.MuteEndTime > DateTimeOffset.UtcNow);
            return (isBanned, isMuted);
        }
        private async Task<bool> HasParentAsync(Guid entityId, EntityType entityType, CancellationToken ct)
        {
            return entityType switch
            {
                EntityType.ProjectFolder => await _context.ProjectFolders
                    .AsNoTracking()
                    .AnyAsync(f => f.Id == entityId && f.ProjectSpaceId != Guid.Empty, ct),
                EntityType.ProjectList => await _context.ProjectLists
                    .AsNoTracking()
                    .AnyAsync(l => l.Id == entityId && (l.ProjectFolderId != null || l.ProjectSpaceId != Guid.Empty), ct),
                _ => await Task.FromResult(false)
            };
        }
        private async Task<Guid?> GetParentIdAsync(Guid entityId, EntityType entityType, CancellationToken ct)
        {
            return entityType switch
            {
                EntityType.ProjectFolder => await _context.ProjectFolders
                    .AsNoTracking()
                    .Where(f => f.Id == entityId)
                    .Select(f => f.ProjectSpaceId)
                    .FirstOrDefaultAsync(ct),
                EntityType.ProjectList => await _context.ProjectLists
                    .AsNoTracking()
                    .Where(l => l.Id == entityId)
                    .Select(l => l.ProjectFolderId)
                    .FirstOrDefaultAsync(ct),
                _ => await Task.FromResult((Guid?)null)
            };
        }

        private async Task<bool> IsMessagePinnedAsync(Guid messageId, CancellationToken ct)
        {
            return await _context.ChatMessages
                .AsNoTracking()
                .AnyAsync(m => m.Id == messageId && m.IsPinned, ct);
        }

        private async Task<bool> IsEntityCreatorAsync(Guid userId, Guid entityId, EntityType entityType, CancellationToken ct)
        {
            var cacheKey = string.Format(EntityCreatorKey, userId, entityId, entityType);

            return await _cache.GetOrCreateAsync(
                cacheKey,
                async factory =>
                {
                    return entityType switch
                    {
                        EntityType.ProjectSpace => await _context.ProjectSpaces.AsNoTracking().AnyAsync(x => x.Id == entityId && x.CreatorId == userId, ct),
                        EntityType.ProjectFolder => await _context.ProjectFolders.AsNoTracking().AnyAsync(x => x.Id == entityId && x.CreatorId == userId, ct),
                        EntityType.ProjectList => await _context.ProjectLists.AsNoTracking().AnyAsync(x => x.Id == entityId && x.CreatorId == userId, ct),
                        EntityType.ProjectTask => await _context.ProjectTasks.AsNoTracking().AnyAsync(x => x.Id == entityId && x.CreatorId == userId, ct),
                        EntityType.ChatRoom => await _context.ChatRooms.AsNoTracking().AnyAsync(x => x.Id == entityId && x.CreatorId == userId, ct),
                        EntityType.ChatMessage => await _context.ChatMessages.AsNoTracking().AnyAsync(x => x.Id == entityId && x.SenderId == userId, ct),
                        _ => false
                    };
                },
                new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) });
        }
    }
}
