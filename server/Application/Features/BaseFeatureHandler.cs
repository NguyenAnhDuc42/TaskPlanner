
using System.ComponentModel.DataAnnotations;
using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Common;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features;

public abstract class BaseFeatureHandler
{
    protected readonly IUnitOfWork UnitOfWork;
    protected readonly ICurrentUserService CurrentUserService;
    protected readonly WorkspaceContext WorkspaceContext;
    protected Guid WorkspaceId => WorkspaceContext.workspaceId;
    protected Guid CurrentUserId => CurrentUserService.CurrentUserId();

    protected BaseFeatureHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
    {
        UnitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        CurrentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        WorkspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
    }

    protected async Task<Entity> GetLayer(Guid layerId, EntityLayerType layerType)
    {
        return layerType switch
        {
            EntityLayerType.ProjectWorkspace =>
                await FindOrThrowAsync<ProjectWorkspace>(layerId),

            EntityLayerType.ProjectSpace =>
                await FindOrThrowAsync<ProjectSpace>(layerId),

            EntityLayerType.ProjectFolder =>
                await FindOrThrowAsync<ProjectFolder>(layerId),

            EntityLayerType.ProjectList =>
                await FindOrThrowAsync<ProjectList>(layerId),

            _ => throw new ArgumentOutOfRangeException(nameof(layerType))
        };
    }

    protected async Task<T> FindOrThrowAsync<T>(Guid id)
    where T : Entity
    {
        var entity = await UnitOfWork.Set<T>()
            .FindAsync(id);

        return entity ?? throw new KeyNotFoundException($"Layer not found: {typeof(T).Name}:{id}");
    }

    /// <summary>
    /// Finds the "Effective Access Layer" for an entity by walking up the hierarchy (Bubble-up).
    /// Returns the LayerId and LayerType of the first Private parent it finds, or the Workspace if all are public.
    /// </summary>
    protected async Task<(Guid Id, EntityLayerType Type)> GetEffectiveAccessLayer(Guid entityId, EntityLayerType type)
    {
        const string sql = @"
            WITH RECURSIVE layer_hierarchy AS (
                -- Starting Layer
                SELECT id, entity_type, parent_id, is_private, 1 AS level
                FROM (
                    SELECT l.id, 'ProjectList' AS entity_type, COALESCE(l.project_folder_id, l.project_space_id) AS parent_id, l.is_private FROM project_lists l WHERE l.id = @EntityId AND @EntityType = 'ProjectList'
                    UNION ALL
                    SELECT f.id, 'ProjectFolder' AS entity_type, f.project_space_id AS parent_id, f.is_private FROM project_folders f WHERE f.id = @EntityId AND @EntityType = 'ProjectFolder'
                    UNION ALL
                    SELECT s.id, 'ProjectSpace' AS entity_type, s.project_workspace_id AS parent_id, s.is_private FROM project_spaces s WHERE s.id = @EntityId AND @EntityType = 'ProjectSpace'
                ) start_layer
                UNION ALL
                -- Walk Up
                SELECT p.id, p.entity_type, p.parent_id, p.is_private, lh.level + 1
                FROM layer_hierarchy lh
                INNER JOIN (
                    SELECT id, 'ProjectFolder' AS entity_type, project_space_id AS parent_id, is_private FROM project_folders
                    UNION ALL
                    SELECT id, 'ProjectSpace' AS entity_type, project_workspace_id AS parent_id, is_private FROM project_spaces
                ) p ON lh.parent_id = p.id AND lh.entity_type <> 'ProjectSpace'
                WHERE lh.is_private = FALSE
            )
            SELECT id, entity_type FROM layer_hierarchy ORDER BY is_private DESC, level ASC LIMIT 1;";

        var result = await UnitOfWork.QuerySingleOrDefaultAsync<dynamic>(sql, new { EntityId = entityId, EntityType = type.ToString() });

        if (result == null) return (WorkspaceId, EntityLayerType.ProjectWorkspace);

        return (result.id, Enum.Parse<EntityLayerType>(result.entity_type));
    }

    /// <summary>
    /// Validates that provided user/member IDs have access to the entity based on "bubble-up" rules.
    /// </summary>
    protected async Task<List<Guid>> GetAccessibleMemberIds(Guid entityId, EntityLayerType type, List<Guid> memberIds)
    {
        if (memberIds == null || !memberIds.Any()) return new List<Guid>();

        var (resolvedId, resolvedType) = await GetEffectiveAccessLayer(entityId, type);

        // If the effective layer is the Workspace, all workspace members are valid
        if (resolvedType == EntityLayerType.ProjectWorkspace)
        {
            return await UnitOfWork.Set<WorkspaceMember>()
                .Where(wm => memberIds.Contains(wm.Id) && wm.ProjectWorkspaceId == WorkspaceId && wm.DeletedAt == null)
                .Select(wm => wm.Id)
                .ToListAsync();
        }

        // Otherwise, filter by explicit EntityAccess on the resolved private layer
        return await UnitOfWork.Set<EntityAccess>()
            .Where(ea => ea.EntityId == resolvedId
                   && ea.EntityLayer == resolvedType
                   && memberIds.Contains(ea.WorkspaceMemberId)
                   && ea.DeletedAt == null)
            .Select(ea => ea.WorkspaceMemberId)
            .ToListAsync();
    }

    protected async Task<(Guid Id, EntityLayerType Type)> GetEffectiveStatusLayer(Guid entityId, EntityLayerType type)
    {
        switch (type)
        {
            case EntityLayerType.ProjectList:
                {
                    var list = await UnitOfWork.Set<ProjectList>()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == entityId);

                    if (list == null) throw new KeyNotFoundException("List not found");
                    if (!list.InheritStatus) return (entityId, type);

                    if (list.ProjectFolderId.HasValue)
                        return await GetEffectiveStatusLayer(list.ProjectFolderId.Value, EntityLayerType.ProjectFolder);

                    return await GetEffectiveStatusLayer(list.ProjectSpaceId, EntityLayerType.ProjectSpace);
                }

            case EntityLayerType.ProjectFolder:
                {
                    var folder = await UnitOfWork.Set<ProjectFolder>()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == entityId);

                    if (folder == null) throw new KeyNotFoundException("Folder not found");
                    if (!folder.InheritStatus) return (entityId, type);

                    return await GetEffectiveStatusLayer(folder.ProjectSpaceId, EntityLayerType.ProjectSpace);
                }

            case EntityLayerType.ProjectSpace:
                return (entityId, type);

            default:
                return (entityId, type);
        }
    }

    protected async Task<Guid?> ResolveTaskStatusId(Guid listId, Guid? requestedStatusId, CancellationToken ct)
    {
        var (effectiveLayerId, effectiveLayerType) =
            await GetEffectiveStatusLayer(listId, EntityLayerType.ProjectList);

        if (requestedStatusId.HasValue)
        {
            var isValidStatus = await UnitOfWork.Set<Status>()
                .AsNoTracking()
                .AnyAsync(
                    s => s.Id == requestedStatusId.Value
                      && s.LayerId == effectiveLayerId
                      && s.LayerType == effectiveLayerType
                      && s.DeletedAt == null,
                    ct);

            if (!isValidStatus)
            {
                throw new ValidationException(
                    "Selected status does not belong to the effective status layer for this task.");
            }

            return requestedStatusId.Value;
        }

        return await UnitOfWork.Set<Status>()
            .AsNoTracking()
            .Where(s =>
                s.LayerId == effectiveLayerId &&
                s.LayerType == effectiveLayerType &&
                s.DeletedAt == null)
            .OrderByDescending(s => s.IsDefaultStatus)
            .ThenBy(s => s.CreatedAt)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(ct);
    }
    protected async Task<List<Guid>> ValidateWorkspaceMembers(List<Guid> userIds, CancellationToken ct)
    {
        if (userIds == null || !userIds.Any())
            return new List<Guid>();

        var validMembers = await UnitOfWork.Set<WorkspaceMember>()
            .Where(wm => userIds.Contains(wm.UserId) && wm.ProjectWorkspaceId == WorkspaceId)
            .Select(wm => wm.UserId)
            .ToListAsync(ct);

        if (validMembers.Count != userIds.Count)
        {
            var invalidIds = userIds.Except(validMembers).ToList();
            throw new ValidationException($"One or more users are not workspace members. Invalid user IDs: {string.Join(", ", invalidIds)}");
        }

        return validMembers;
    }

    protected async Task<Guid> GetWorkspaceMemberId(Guid userId, CancellationToken ct = default)
    {
        var member = await UnitOfWork.Set<WorkspaceMember>()
            .AsNoTracking()
            .FirstOrDefaultAsync(wm => wm.UserId == userId && wm.ProjectWorkspaceId == WorkspaceId, ct);

        return member?.Id ?? throw new KeyNotFoundException($"User {userId} is not a member of workspace {WorkspaceId}");
    }

    protected async Task<List<Guid>> GetWorkspaceMemberIds(List<Guid> userIds, CancellationToken ct = default)
    {
        if (userIds == null || !userIds.Any()) return new List<Guid>();

        var members = await UnitOfWork.Set<WorkspaceMember>()
            .AsNoTracking()
            .Where(wm => userIds.Contains(wm.UserId) && wm.ProjectWorkspaceId == WorkspaceId)
            .Select(wm => wm.Id)
            .ToListAsync(ct);

        if (members.Count != userIds.Count)
        {
            throw new ValidationException("One or more users are not members of this workspace.");
        }

        return members;
    }

    protected IQueryable<T> QueryNoTracking<T>() where T : class
    {
        return UnitOfWork.Set<T>().AsNoTracking();
    }
}
