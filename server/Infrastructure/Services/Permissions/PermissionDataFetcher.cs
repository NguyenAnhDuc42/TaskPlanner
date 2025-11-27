using Domain.Common;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Domain.Enums.Workspace;
using Domain.Entities.ProjectEntities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Permissions;

public class PermissionDataFetcher
{
    private readonly TaskPlanDbContext _dbContext;

    public PermissionDataFetcher(TaskPlanDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Role> GetWorkspaceRoleAsync(Guid userId, Guid workspaceId, CancellationToken ct)
    {
        var result = await _dbContext.WorkspaceMembers
            .Where(wm => wm.ProjectWorkspaceId == workspaceId && wm.UserId == userId)
            .Select(wm => wm.Role)
            .FirstOrDefaultAsync(ct);
        return result;
    }

    public async Task<AccessLevel?> GetEntityAccessLevelAsync(Guid userId, Guid entityId, EntityType entityType, CancellationToken ct)
    {
        var result = await _dbContext.EntityMembers
            .Where(em => em.LayerId == entityId && em.UserId == userId && em.LayerType.ToString() == entityType.ToString())
            .Select(em => (AccessLevel?)em.AccessLevel)
            .FirstOrDefaultAsync(ct);
        return result;
    }

    public async Task<ChatRoomRole?> GetChatRoomRoleAsync(Guid userId, Guid chatRoomId, CancellationToken ct)
    {
        return await _dbContext.ChatRoomMembers
            .Where(crm => crm.ChatRoomId == chatRoomId && crm.UserId == userId)
            .Select(crm => (ChatRoomRole?)crm.Role)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<(bool IsBanned, bool IsMuted)> GetChatRoomMemberStatusAsync(Guid userId, Guid chatRoomId, CancellationToken ct)
    {
        var member = await _dbContext.ChatRoomMembers
            .Where(crm => crm.ChatRoomId == chatRoomId && crm.UserId == userId)
            .Select(crm => new { crm.IsBanned, crm.IsMuted, crm.MuteEndTime })
            .FirstOrDefaultAsync(ct);

        if (member == null) return (false, false);

        var isMuted = member.IsMuted && (member.MuteEndTime == null || member.MuteEndTime > DateTimeOffset.UtcNow);
        return (member.IsBanned, isMuted);
    }

    public async Task<(Role?, AccessLevel?)> GetLayeredPermissionsAsync<TEntity>(Guid userId, Guid workspaceId, TEntity entity, CancellationToken ct) 
        where TEntity : Entity
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));

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

    public static EntityType GetEntityType<TEntity>() where TEntity : Entity =>
        typeof(TEntity).Name switch
        {
            nameof(ProjectWorkspace) => EntityType.ProjectWorkspace,
            nameof(ProjectSpace) => EntityType.ProjectSpace,
            nameof(ProjectFolder) => EntityType.ProjectFolder,
            nameof(ProjectList) => EntityType.ProjectList,
            _ => throw new InvalidOperationException($"Unknown entity type: {typeof(TEntity).Name}")
        };
}
