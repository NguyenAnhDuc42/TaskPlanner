using System;
using Application.Common;
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

    /// <summary>
    /// Builds complete permission context from an entity. Use this for permission checks when you already have the entity.
    /// </summary>
    public async Task<PermissionContext> BuildFromEntityAsync<TEntity>(
        Guid userId,
        Guid workspaceId,
        TEntity entity,
        EntityType entityType,
        CancellationToken ct) where TEntity : IIdentifiable
    {
        var entityId = GetEntityId(entity);
        var workspaceRole = await GetWorkspaceRoleAsync(userId, workspaceId, ct);
        var isCreator = ExtractCreator(entity, userId);
        var (isArchived, isPrivate) = ExtractEntityState(entity);

        var chatRoomRole = entityType == EntityType.ChatRoom && entity is ChatRoom chatRoom
            ? await GetChatRoomRoleAsync(userId, chatRoom.Id, ct)
            : null;

        var (isBanned, isMuted) = entityType == EntityType.ChatRoom
            ? await GetChatRoomMemberStatusAsync(userId, entityId, ct)
            : (false, false);

        var entityAccess = await GetEntityAccessLevelAsync(userId, entityId, entityType, ct);

        return new PermissionContext
        {
            UserId = userId,
            WorkspaceId = workspaceId,
            EntityId = entityId,
            EntityType = entityType,
            WorkspaceRole = workspaceRole,
            IsWorkspaceOwner = workspaceRole == Role.Owner,
            IsWorkspaceAdmin = workspaceRole == Role.Admin,
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
        };
    }

    // ============ Entity Extraction Helpers ============

    private static Guid GetEntityId<TEntity>(TEntity entity) where TEntity : IIdentifiable =>
        entity.Id;

    private static bool ExtractCreator<TEntity>(TEntity entity, Guid userId) where TEntity : IIdentifiable =>
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

    private static (bool IsArchived, bool IsPrivate) ExtractEntityState<TEntity>(TEntity entity) where TEntity : IIdentifiable =>
        entity switch
        {
            ChatRoom cr => (cr.IsArchived, cr.IsPrivate),
            ProjectTask pt => (pt.IsArchived, false),
            ProjectList pl => (pl.IsArchived, false),
            ProjectFolder pf => (pf.IsArchived, false),
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

    private async Task<AccessLevel?> GetEntityAccessLevelAsync(Guid userId, Guid entityId, EntityType entityType, CancellationToken ct)
    {
        var cacheKey = string.Format(EntityMemberPermissionKey, userId, entityId, entityType);

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async factory =>
            {
                var em = await _context.EntityMembers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.EntityId == entityId &&
                        x.EntityType.ToString() == entityType.ToString() &&
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
