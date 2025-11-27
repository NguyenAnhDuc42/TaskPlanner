using Application.Common;
using Domain.Common;
using Domain.Enums;
using Domain.Enums.Workspace;
using Domain.Entities.Support.Workspace;

namespace Infrastructure.Services.Permissions;

public class PermissionContextBuilder
{
    private readonly PermissionDataFetcher _dataFetcher;

    public PermissionContextBuilder(PermissionDataFetcher dataFetcher)
    {
        _dataFetcher = dataFetcher;
    }

    // Variant 1: Build context for entity itself
    public async Task<PermissionContext> BuildForEntityAsync<TEntity>(
        Guid workspaceId,
        Guid userId,
        TEntity entity,
        PermissionDataNeeds needs,
        CancellationToken ct) where TEntity : Entity
    {
        var entityType = PermissionDataFetcher.GetEntityType<TEntity>();

        // Selectively fetch based on needs
        Role? role = null;
        AccessLevel? access = null;
        if (needs.HasFlag(PermissionDataNeeds.WorkspaceRole) || needs.HasFlag(PermissionDataNeeds.EntityAccess))
        {
            (role, access) = await _dataFetcher.GetLayeredPermissionsAsync(userId, workspaceId, entity, ct);
        }

        bool isCreator = needs.HasFlag(PermissionDataNeeds.IsCreator)
            ? PermissionDataFetcher.ExtractCreator(entity, userId)
            : false;

        bool isArchived = false, isPrivate = false;
        if (needs.HasFlag(PermissionDataNeeds.EntityState))
        {
            (isArchived, isPrivate) = PermissionDataFetcher.ExtractEntityState(entity);
        }

        return new PermissionContext
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
    }

    // Variant 2: Build context for creating child in parent
    public async Task<PermissionContext> BuildForParentChildAsync<TParent>(
        Guid workspaceId,
        Guid userId,
        TParent parentEntity,
        PermissionDataNeeds needs,
        CancellationToken ct) where TParent : Entity
    {
        var parentType = PermissionDataFetcher.GetEntityType<TParent>();

        // Fetch from parent context based on child rule needs
        Role? role = null;
        AccessLevel? access = null;
        if (needs.HasFlag(PermissionDataNeeds.WorkspaceRole) || needs.HasFlag(PermissionDataNeeds.EntityAccess))
        {
            (role, access) = await _dataFetcher.GetLayeredPermissionsAsync(userId, workspaceId, parentEntity, ct);
        }

        bool isPrivate = false;
        if (needs.HasFlag(PermissionDataNeeds.EntityState))
        {
            (_, isPrivate) = PermissionDataFetcher.ExtractEntityState(parentEntity);
        }

        return new PermissionContext
        {
            UserId = userId,
            WorkspaceId = workspaceId,
            EntityId = parentEntity.Id,
            EntityType = parentType,
            WorkspaceRole = role ?? Role.Guest,
            EntityAccess = access,
            IsEntityPrivate = isPrivate
        };
    }

    // Variant 3: Build context for child with parent context (ChatRoom, etc.)
    public async Task<PermissionContext> BuildForChildWithParentAsync<TChild, TParent>(
        Guid workspaceId,
        Guid userId,
        TChild childEntity,
        TParent parentEntity,
        PermissionDataNeeds needs,
        CancellationToken ct) 
        where TChild : Entity
        where TParent : Entity
    {
        var childType = PermissionDataFetcher.GetEntityType<TChild>();

        // Fetch based on rule needs
        Role? role = null;
        AccessLevel? access = null;
        if (needs.HasFlag(PermissionDataNeeds.WorkspaceRole) || needs.HasFlag(PermissionDataNeeds.EntityAccess))
        {
            (role, access) = await _dataFetcher.GetLayeredPermissionsAsync(userId, workspaceId, childEntity, ct);
        }

        bool isCreator = needs.HasFlag(PermissionDataNeeds.IsCreator)
            ? PermissionDataFetcher.ExtractCreator(childEntity, userId)
            : false;

        bool isArchived = false, isPrivate = false;
        if (needs.HasFlag(PermissionDataNeeds.EntityState))
        {
            (isArchived, isPrivate) = PermissionDataFetcher.ExtractEntityState(childEntity);
        }

        // ONLY fetch ChatRoom data if needed AND parent is ChatRoom
        ChatRoomRole? chatRole = null;
        bool isBanned = false, isMuted = false;
        if (parentEntity is ChatRoom cr &&
            (needs.HasFlag(PermissionDataNeeds.ChatRoomRole) || needs.HasFlag(PermissionDataNeeds.ChatRoomMemberStatus)))
        {
            if (needs.HasFlag(PermissionDataNeeds.ChatRoomRole))
            {
                chatRole = await _dataFetcher.GetChatRoomRoleAsync(userId, cr.Id, ct);
            }

            if (needs.HasFlag(PermissionDataNeeds.ChatRoomMemberStatus))
            {
                var status = await _dataFetcher.GetChatRoomMemberStatusAsync(userId, cr.Id, ct);
                isBanned = status.IsBanned;
                isMuted = status.IsMuted;
            }
        }

        return new PermissionContext
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
    }
}
