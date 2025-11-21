using System;
using Application.Common;
using Domain.Common;
using Domain.Common.Interfaces;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Entities.Support.Workspace;
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
    public async Task<PermissionContext> BuildFromEntityAsync<TEntity>(Guid userId, Guid workspaceId, TEntity entity, CancellationToken ct = default) where TEntity : Entity
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        ct.ThrowIfCancellationRequested();
        var context = new PermissionContext
        {
            UserId = userId,
            WorkspaceId = workspaceId
        };
        context.IsUserSuspendedInWorkspace = await IsUserSuspendedInWorkspaceAsync(userId, workspaceId, ct);
        async Task EnsureWorkspaceRoleFallbackAsync()
        {
            if (context.WorkspaceRole == default) // not set yet
                context.WorkspaceRole = await GetWorkspaceRoleAsync(userId, workspaceId, ct);
        }
        switch (entity)
        {
            case ProjectWorkspace:
                context.WorkspaceRole = await GetWorkspaceRoleAsync(userId, entity.Id, ct);
                break;
            case ProjectSpace:
                context.EntityId = entity.Id;
                context.EntityType = EntityType.ProjectSpace;
                context.EntityAccess = await GetEntityAccessLevelAsync(userId, entity.Id, EntityType.ProjectSpace, ct);
                if (context.EntityAccess == null) await EnsureWorkspaceRoleFallbackAsync();

                break;
            case ProjectFolder:
                context.EntityId = entity.Id;
                context.EntityType = EntityType.ProjectFolder;
                context.EntityAccess = await GetEntityAccessLevelAsync(userId, entity.Id, EntityType.ProjectFolder, ct);
                if (context.EntityAccess == null) await EnsureWorkspaceRoleFallbackAsync();
                break;
            case ProjectList:
                context.EntityId = entity.Id;
                context.EntityType = EntityType.ProjectList;
                context.EntityAccess = await GetEntityAccessLevelAsync(userId, entity.Id, EntityType.ProjectList, ct);
                if (context.EntityAccess == null) await EnsureWorkspaceRoleFallbackAsync();
                break;
            case ChatRoom:
                context.EntityId = entity.Id;
                context.EntityType = EntityType.ChatRoom;
                context.ChatRoomRole = await GetChatRoomRoleAsync(userId, entity.Id, ct);
                var chatRoomState = await GetChatRoomMemberStatusAsync(userId, entity.Id, ct);
                context.IsUserBannedFromChatRoom = chatRoomState.IsBanned;
                context.IsUserMutedInChatRoom = chatRoomState.IsMuted;
                if (context.ChatRoomRole == null) await EnsureWorkspaceRoleFallbackAsync();
                break;
            default:
                context.WorkspaceRole = await GetWorkspaceRoleAsync(userId, workspaceId, ct);
                break;
        }
        context.IsCreator = ExtractCreator(entity, userId);
        var state = ExtractEntityState(entity);
        context.IsEntityArchived = state.IsArchived;
        context.IsEntityPrivate = state.IsPrivate;
        return context;
    }


    public async Task<PermissionContext> BuildScopeContextAsync(Guid userId, Guid workspaceId, Guid? layerId, EntityType entityType, CancellationToken ct)
    {
        var context = new PermissionContext
        {
            UserId = userId,
            WorkspaceId = workspaceId
        };

        context.IsUserSuspendedInWorkspace = await IsUserSuspendedInWorkspaceAsync(userId, workspaceId, ct);
        context.WorkspaceRole = await GetWorkspaceRoleAsync(userId, workspaceId, ct);

        if (layerId.HasValue)
        {
            context.EntityId = layerId.Value;
            context.EntityType = entityType;
            context.EntityAccess = await GetEntityAccessLevelAsync(userId, layerId.Value, entityType, ct);
            
            if (entityType == EntityType.ChatRoom)
            {
                context.ChatRoomRole = await GetChatRoomRoleAsync(userId, layerId.Value, ct);
                var chatRoomState = await GetChatRoomMemberStatusAsync(userId, layerId.Value, ct);
                context.IsUserBannedFromChatRoom = chatRoomState.IsBanned;
                context.IsUserMutedInChatRoom = chatRoomState.IsMuted;
            }
        }

        return context;
    }

    // ============ Entity Extraction Helpers ============
    private static Guid GetEntityId<TEntity>(TEntity entity) where TEntity : class =>
        entity switch
        {
            ChatRoom cr => cr.Id,
            ChatMessage cm => cm.Id,
            ProjectTask pt => pt.Id,
            ProjectList pl => pl.Id,
            ProjectFolder pf => pf.Id,
            ProjectSpace ps => ps.Id,
            ProjectWorkspace pw => pw.Id,
            WorkspaceMember wm => wm.Id,
            ChatRoomMember crm => crm.Id,
            _ => Guid.Empty
        };
    private static bool ExtractCreator<TEntity>(TEntity entity, Guid userId) where TEntity : class =>
        entity switch
        {
            ChatRoom cr => cr.CreatorId == userId,
            ChatMessage cm => cm.SenderId == userId,
            ProjectTask pt => pt.CreatorId == userId,
            ProjectList pl => pl.CreatorId == userId,
            ProjectFolder pf => pf.CreatorId == userId,
            ProjectSpace ps => ps.CreatorId == userId,
            ProjectWorkspace pw => pw.CreatorId == userId,
            WorkspaceMember wm => wm.CreatedBy == userId,
            ChatRoomMember crm => crm.CreatedBy == userId,
            _ => false
        };

    private static (bool IsArchived, bool IsPrivate) ExtractEntityState<TEntity>(TEntity entity) where TEntity : class =>
        entity switch
        {
            ChatRoom cr => (cr.IsArchived, cr.IsPrivate),
            ProjectTask pt => (pt.IsArchived, false),
            ProjectList pl => (pl.IsArchived, pl.IsPrivate),
            ProjectFolder pf => (pf.IsArchived, pf.IsPrivate),
            ProjectSpace ps => (ps.IsArchived, ps.IsPrivate),
            ProjectWorkspace pw => (pw.IsArchived, false),
            WorkspaceMember _ => (false, false),
            ChatRoomMember _ => (false, false),
            _ => (false, false)
        };

    // ============ Database Fetching Methods ============

    private async Task<Role> GetWorkspaceRoleAsync(Guid userId, Guid workspaceId, CancellationToken ct)
    {
        var cacheKey = string.Format(WorkspaceMemberKey, userId, workspaceId);

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async factory =>
            {
                var wm = await _context.WorkspaceMembers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.ProjectWorkspaceId == workspaceId &&
                        x.UserId == userId, ct);
                return wm?.Role ?? Role.Guest;
            },
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) });
    }

    private async Task<AccessLevel?> GetEntityAccessLevelAsync(Guid userId, Guid layerId, EntityType layerType, CancellationToken ct)
    {
        var cacheKey = string.Format(EntityMemberPermissionKey, userId, layerId, layerType);

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async factory =>
            {
                var em = await _context.EntityMembers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.LayerId == layerId &&
                        x.LayerType.ToString() == layerType.ToString() &&
                        x.UserId == userId, ct);
                return em?.AccessLevel;
            },
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) });
    }

    private async Task<ChatRoomRole?> GetChatRoomRoleAsync(Guid userId, Guid chatRoomId, CancellationToken ct)
    {
        var cacheKey = string.Format(ChatRoomMemberKey, userId, chatRoomId);

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async factory =>
            {
                var crm = await _context.ChatRoomMembers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.ChatRoomId == chatRoomId &&
                        x.UserId == userId, ct);
                return crm?.Role;
            },
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) });
    }

    private async Task<bool> IsUserSuspendedInWorkspaceAsync(Guid userId, Guid workspaceId, CancellationToken ct)
    {
        return await _context.WorkspaceMembers
            .AsNoTracking()
            .AnyAsync(wm =>
                wm.ProjectWorkspaceId == workspaceId &&
                wm.UserId == userId &&
                wm.Status == MembershipStatus.Suspended, ct);
    }

    private async Task<(bool IsBanned, bool IsMuted)> GetChatRoomMemberStatusAsync(Guid userId, Guid chatRoomId, CancellationToken ct)
    {
        var member = await _context.ChatRoomMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(crm =>
                crm.UserId == userId &&
                crm.ChatRoomId == chatRoomId, ct);

        if (member == null)
            return (false, false);

        var isBanned = member.IsBanned;
        var isMuted = member.IsMuted && (member.MuteEndTime == null || member.MuteEndTime > DateTimeOffset.UtcNow);
        return (isBanned, isMuted);
    }
}
