using Domain.Common;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Domain.Enums.Workspace;
using Domain.Entities.ProjectEntities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Dapper;

using Application.Common;

namespace Infrastructure.Services.Permissions;

public class PermissionDataFetcher
{
    private readonly TaskPlanDbContext _dbContext;
    private readonly HybridCache _cache;

    public PermissionDataFetcher(TaskPlanDbContext dbContext, HybridCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<Role> GetWorkspaceRoleAsync(Guid userId, Guid workspaceId, CancellationToken ct)
    {
        var cacheKey = CacheConstants.Keys.WorkspaceMemberRole(userId, workspaceId);
        return await _cache.GetOrCreateAsync(
            cacheKey,
            async cancel =>
            {
                Console.WriteLine($"[HybridCache] MISS: Fetching workspace role for user {userId} and workspace {workspaceId}");
                
                const string sql = "SELECT role FROM workspace_members WHERE user_id = @userId AND project_workspace_id = @workspaceId AND deleted_at IS NULL AND status = 'Active' LIMIT 1";
                var conn = _dbContext.Database.GetDbConnection();
                var roleStr = await conn.QueryFirstOrDefaultAsync<string>(sql, new { userId, workspaceId });
                
                return Enum.TryParse<Role>(roleStr, out var role) ? role : Role.None;
            },
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(10) },
            tags: new[] { CacheConstants.Tags.UserPermissions(userId), CacheConstants.Tags.WorkspaceMembers(workspaceId) },
            cancellationToken: ct);
    }

    public async Task<AccessLevel?> GetEntityAccessLevelAsync(Guid userId, Guid workspaceId, Guid entityId, EntityType entityType, CancellationToken ct)
    {
        var cacheKey = CacheConstants.Keys.EntityAccessLevel(userId, workspaceId, entityId, entityType.ToString());
        return await _cache.GetOrCreateAsync(
            cacheKey,
            async cancel =>
            {
                Console.WriteLine($"[HybridCache] MISS: Fetching entity access level for user {userId} and entity {entityId} ({entityType})");
                
                const string sql = "SELECT access_level FROM entity_members WHERE user_id = @userId AND layer_id = @entityId AND layer_type = @entityTypeStr AND deleted_at IS NULL LIMIT 1";
                var conn = _dbContext.Database.GetDbConnection();
                var accessStr = await conn.QueryFirstOrDefaultAsync<string>(sql, new { userId, entityId, entityTypeStr = entityType.ToString() });
                
                return Enum.TryParse<AccessLevel>(accessStr, out var access) ? access : (AccessLevel?)null;
            },
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(10) },
            tags: new[] { CacheConstants.Tags.UserPermissions(userId), CacheConstants.Tags.WorkspaceMembers(workspaceId) },
            cancellationToken: ct);
    }

    public async Task<ChatRoomRole?> GetChatRoomRoleAsync(Guid userId, Guid workspaceId, Guid chatRoomId, CancellationToken ct)
    {
        var cacheKey = CacheConstants.Keys.ChatRoomRole(userId, workspaceId, chatRoomId);
        return await _cache.GetOrCreateAsync(
            cacheKey,
            async cancel =>
            {
                Console.WriteLine($"[HybridCache] MISS: Fetching chat room role for user {userId} and chat room {chatRoomId}");
                var result = await _dbContext.ChatRoomMembers
                    .AsNoTracking()
                    .Where(crm => crm.ChatRoomId == chatRoomId && crm.UserId == userId)
                    .Select(crm => (ChatRoomRole?)crm.Role)
                    .FirstOrDefaultAsync(ct);
                return result;
            },
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(10) },
            tags: new[] { CacheConstants.Tags.UserPermissions(userId), CacheConstants.Tags.WorkspaceMembers(workspaceId) },
            cancellationToken: ct);
    }

    public record ChatMemberStatus(bool IsBanned, bool IsMuted);

    public async Task<PermissionContext> GetSecurityContextAsync(Guid userId, HierarchyPath path, CancellationToken ct)
    {
        // 1. Fetch Workspace Role
        var role = await GetWorkspaceRoleAsync(userId, path.WorkspaceId, ct);

        // 2. Fetch Entity Members for all IDs in the path
        var allIds = path.Ancestors.Select(a => a.Id).ToList();
        allIds.Add(path.TargetId);

        const string sql = @"
            SELECT layer_id as LayerId, access_level as AccessLevel 
            FROM entity_members 
            WHERE user_id = @userId AND layer_id = ANY(@allIds) AND deleted_at IS NULL";

        var conn = _dbContext.Database.GetDbConnection();
        var rawOverrides = await conn.QueryAsync<dynamic>(sql, new { userId, allIds = allIds.ToArray() });
        var overrides = rawOverrides.ToDictionary(
            x => (Guid)x.layerid,
            x => Enum.TryParse<AccessLevel>(x.accesslevel.ToString(), out AccessLevel al) ? (AccessLevel?)al : null);

        // 3. Resolve "Effective Access" and "Privacy Block" (The Private-First Waterfall)
        var visibilityChain = new List<VisibilityLevel>();
        AccessLevel? effectiveAccess = null;
        bool isPrivacyBlocked = false;

        // A. Check TARGET first
        overrides.TryGetValue(path.TargetId, out var targetOverride);
        var targetLevel = new VisibilityLevel
        {
            Id = path.TargetId,
            Type = path.TargetType,
            IsPrivate = path.TargetIsPrivate,
            HasExplicitMembership = targetOverride.HasValue,
            Access = targetOverride
        };
        visibilityChain.Add(targetLevel);
        
        if (targetOverride.HasValue) effectiveAccess = targetOverride;
        if (targetLevel.IsBlocking) isPrivacyBlocked = true;

        // B. Walk ANCESTORS (from bottom to top)
        if (!isPrivacyBlocked)
        {
            foreach (var node in path.Ancestors)
            {
                overrides.TryGetValue(node.Id, out var nodeOverride);
                var level = new VisibilityLevel
                {
                    Id = node.Id,
                    Type = node.Type,
                    IsPrivate = node.IsPrivate,
                    HasExplicitMembership = nodeOverride.HasValue,
                    Access = nodeOverride
                };
                visibilityChain.Add(level);

                // Waterfall inheritance: use the most specific (lowest) access found
                if (nodeOverride.HasValue) effectiveAccess ??= nodeOverride;

                if (level.IsBlocking)
                {
                    isPrivacyBlocked = true;
                    break;
                }
            }
        }

        var context = new PermissionContext
        {
            UserId = userId,
            WorkspaceId = path.WorkspaceId,
            EntityType = path.TargetType,
            EntityId = path.TargetId,
            WorkspaceRole = role,
            EntityAccess = effectiveAccess,
            VisibilityChain = visibilityChain,
            IsPrivacyBlocked = isPrivacyBlocked,
            IsEntityPrivate = path.TargetIsPrivate,
            IsCreator = path.TargetCreatorId == userId,
            IsEntityArchived = path.TargetIsArchived
        };

        // Path Integrity Validation (Hardening)
        bool isPathCorrupted = path.TargetType switch
        {
            EntityType.ProjectTask => path.Ancestors.Count < 3, // List, Folder?, Space
            EntityType.ProjectList => path.Ancestors.Count < 1, // Folder?, Space
            EntityType.ProjectFolder => path.Ancestors.Count < 1, // Space
            _ => false
        };

        if (isPathCorrupted)
        {
            context.IsPrivacyBlocked = true;
        }

        // Special handling for Chat (status check)
        if (path.TargetType == EntityType.ChatRoom || path.TargetType == EntityType.ChatMessage)
        {
            var chatRoomId = path.TargetType == EntityType.ChatRoom 
                ? path.TargetId 
                : path.Ancestors.FirstOrDefault(a => a.Type == EntityType.ChatRoom)?.Id;

            if (chatRoomId.HasValue)
            {
                var (isBanned, isMuted) = await GetChatRoomMemberStatusAsync(userId, path.WorkspaceId, chatRoomId.Value, ct);
                context.IsUserBannedFromChatRoom = isBanned;
                context.IsUserMutedInChatRoom = isMuted;
                context.ChatRoomRole = await GetChatRoomRoleAsync(userId, path.WorkspaceId, chatRoomId.Value, ct);
                context.IsChatRoomOwner = context.ChatRoomRole == ChatRoomRole.Owner;
            }
        }

        return context;
    }

    public async Task<(bool IsBanned, bool IsMuted)> GetChatRoomMemberStatusAsync(Guid userId, Guid workspaceId, Guid chatRoomId, CancellationToken ct)
    {
        var cacheKey = CacheConstants.Keys.ChatMemberStatus(userId, workspaceId, chatRoomId);
        var status = await _cache.GetOrCreateAsync(
            cacheKey,
            async cancel =>
            {
                var member = await _dbContext.ChatRoomMembers
                    .AsNoTracking()
                    .Where(crm => crm.ChatRoomId == chatRoomId && crm.UserId == userId)
                    .Select(crm => new { crm.IsBanned, crm.IsMuted, crm.MuteEndTime })
                    .FirstOrDefaultAsync(ct);

                if (member == null) return new ChatMemberStatus(false, false);

                var isMuted = member.IsMuted && (member.MuteEndTime == null || member.MuteEndTime > DateTimeOffset.UtcNow);
                return new ChatMemberStatus(member.IsBanned, isMuted);
            },
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) },
            tags: new[] { CacheConstants.Tags.UserPermissions(userId), CacheConstants.Tags.WorkspaceMembers(workspaceId) },
            cancellationToken: ct);

        return (status.IsBanned, status.IsMuted);
    }

    public async Task<(Role?, AccessLevel?)> GetLayeredPermissionsAsync<TEntity>(Guid userId, Guid workspaceId, TEntity entity, CancellationToken ct) 
        where TEntity : Entity
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        // 1. Determine the hierarchy for waterfalling
        Guid? listId = null;
        Guid? folderId = null;
        Guid? spaceId = null;

        switch (entity)
        {
            case ProjectWorkspace:
                var wsRole = await GetWorkspaceRoleAsync(userId, workspaceId, ct);
                return (wsRole, null);
            case ProjectSpace ps:
                spaceId = ps.Id;
                break;
            case ProjectFolder pf:
                folderId = pf.Id;
                spaceId = pf.ProjectSpaceId;
                break;
            case ProjectList pl:
                listId = pl.Id;
                folderId = pl.ProjectFolderId; // might be null
                spaceId = pl.ProjectSpaceId;
                break;
            case ProjectTask pt:
                // For tasks, we use the fallback to the workspace role for now, 
                // but usually tasks inherit from the List.
                // If needed, we could load the List here too.
                break;
            default:
                var defaultRole = await GetWorkspaceRoleAsync(userId, workspaceId, ct);
                return (defaultRole, null);
        }

        // 2. Fetch all potential overrides in one go (The Waterfall Query)
        const string sql = @"
            SELECT layer_id as LayerId, layer_type as LayerType, access_level as AccessLevel 
            FROM entity_members 
            WHERE user_id = @userId AND deleted_at IS NULL AND (
                (layer_id = @listId AND layer_type = 'ProjectList') OR
                (layer_id = @folderId AND layer_type = 'ProjectFolder') OR
                (layer_id = @spaceId AND layer_type = 'ProjectSpace')
            )";

        var conn = _dbContext.Database.GetDbConnection();
        var overrides = (await conn.QueryAsync<dynamic>(sql, new { userId, listId, folderId, spaceId })).ToList();

        // 3. Pick the most specific (List > Folder > Space)
        if (listId.HasValue)
        {
            var match = overrides.FirstOrDefault(o => (Guid)o.LayerId == listId.Value);
            if (match != null) return (null, (AccessLevel)Enum.Parse<AccessLevel>(match.AccessLevel));
        }
        if (folderId.HasValue)
        {
            var match = overrides.FirstOrDefault(o => (Guid)o.LayerId == folderId.Value);
            if (match != null) return (null, (AccessLevel)Enum.Parse<AccessLevel>(match.AccessLevel));
        }
        if (spaceId.HasValue)
        {
            var match = overrides.FirstOrDefault(o => (Guid)o.LayerId == spaceId.Value);
            if (match != null) return (null, (AccessLevel)Enum.Parse<AccessLevel>(match.AccessLevel));
        }

        // 4. Fallback to Workspace Role
        var role = await GetWorkspaceRoleAsync(userId, workspaceId, ct);
        return (role, null);
    }

    public static bool ExtractCreator<TEntity>(TEntity entity, Guid userId) where TEntity : Entity =>
        entity switch
        {
            ProjectWorkspace pw => pw.CreatorId == userId,
            ProjectSpace ps => ps.CreatorId == userId,
            ProjectFolder pf => pf.CreatorId == userId,
            ProjectList pl => pl.CreatorId == userId,
            _ => false
        };

    public static (bool IsArchived, bool IsPrivate) ExtractEntityState<TEntity>(TEntity entity) where TEntity : Entity =>
        entity switch
        {
            ProjectSpace ps => (ps.IsArchived, ps.IsPrivate),
            ProjectFolder pf => (pf.IsArchived, pf.IsPrivate),
            ProjectList pl => (pl.IsArchived, pl.IsPrivate),
            _ => (false, false)
        };


    // Simplified tag-based invalidation
    public async Task InvalidateWorkspaceRoleCacheAsync(Guid userId, Guid workspaceId)
    {
        // Blow away everything for this workspace's members to be safe, 
        // or just the specific user's permissions
        await _cache.RemoveByTagAsync(CacheConstants.Tags.WorkspaceMembers(workspaceId));
    }

    public async Task InvalidateUserPermissionsCacheAsync(Guid userId)
    {
        await _cache.RemoveByTagAsync(CacheConstants.Tags.UserPermissions(userId));
    }

    public async Task InvalidateChatRoomCacheAsync(Guid userId, Guid chatRoomId)
    {
        // Simple wipe for now, tag-based handles it
        await InvalidateUserPermissionsCacheAsync(userId);
    }
}

