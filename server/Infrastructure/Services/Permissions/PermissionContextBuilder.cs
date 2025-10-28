using System;
using Application.Common;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Domain.Enums.Workspace;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Infrastructure.Services.Permissions;

public class PermissionContextBuilder
{
    private readonly TaskPlanDbContext _context;
    private readonly HybridCache _cache;

    private const string WorkspaceMemberKey = "workspace_member_{0}_{1}";
    private const string EntityMemberPermissionKey = "entity_member_{0}_{1}_{2}";
    private const string ChatRoomMemberKey = "chat_room_member_{0}_{1}";
    private const string EntityCreatorKey = "entity_creator_{0}_{1}_{2}";

    public PermissionContextBuilder(TaskPlanDbContext context, HybridCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<PermissionContext> BuildMinimalAsync(Guid userId, Guid workspaceId, Guid? entityId, EntityType entityType, CancellationToken ct)
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
            IsEntityArchived = false,  
            IsEntityPrivate = false,   // Default, can be fetched separately if needed
        };
    }
    public async Task<PermissionContext> WithEntityStateAsync(PermissionContext context, CancellationToken ct)
    {
        if (!context.EntityId.HasValue)
            return context;

        var (isArchived, isPrivate) = await GetEntityStateAsync(context.EntityId.Value, context.EntityType, ct);
        return context with { IsEntityArchived = isArchived, IsEntityPrivate = isPrivate };
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
    private async Task<(bool IsArchived, bool IsPrivate)> GetEntityStateAsync(
            Guid entityId,
            EntityType entityType,
            CancellationToken ct)
    {
        var defaultState = (false, false);

        return entityType switch
        {
            EntityType.ProjectTask => await GetTaskStateAsync(),
            EntityType.ProjectList => await GetListStateAsync(),
            EntityType.ChatRoom => await GetChatRoomStateAsync(),
            _ => await Task.FromResult(defaultState)
        };

        async Task<(bool IsArchived, bool IsPrivate)> GetTaskStateAsync()
        {
            var result = await _context.ProjectTasks
                .AsNoTracking()
                .Where(t => t.Id == entityId)
                .Select(t => new { t.IsArchived })
                .FirstOrDefaultAsync(ct);

            return result != null ? (result.IsArchived, false) : defaultState;
        }

        async Task<(bool IsArchived, bool IsPrivate)> GetListStateAsync()
        {
            var result = await _context.ProjectLists
                .AsNoTracking()
                .Where(l => l.Id == entityId)
                .Select(l => new { l.IsArchived })
                .FirstOrDefaultAsync(ct);

            return result != null ? (result.IsArchived, false) : defaultState;
        }

        async Task<(bool IsArchived, bool IsPrivate)> GetChatRoomStateAsync()
        {
            var result = await _context.ChatRooms
                .AsNoTracking()
                .Where(cr => cr.Id == entityId)
                .Select(cr => new { cr.IsArchived, cr.IsPrivate })
                .FirstOrDefaultAsync(ct);

            return result != null ? (result.IsArchived, result.IsPrivate) : defaultState;
        }
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
