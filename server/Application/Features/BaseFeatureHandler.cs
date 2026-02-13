using Application.Common;
using Application.Common.Exceptions;
using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Common;
using Domain.Entities.ProjectEntities;
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
    /// Validates that all provided user IDs are members of the current workspace.
    /// Returns the list of valid user IDs.
    /// Throws ValidationException if any user is not a workspace member.
    /// </summary>
    protected async Task<List<Guid>> ValidateWorkspaceMembers(List<Guid> userIds, CancellationToken ct)
    {
        if (userIds == null || !userIds.Any())
            return new List<Guid>();

        var validMembers = await UnitOfWork.Set<Domain.Entities.Relationship.WorkspaceMember>()
            .Where(wm => userIds.Contains(wm.UserId) && wm.ProjectWorkspaceId == WorkspaceId)
            .Select(wm => wm.UserId)
            .ToListAsync(ct);

        if (validMembers.Count != userIds.Count)
        {
            var invalidIds = userIds.Except(validMembers).ToList();
            throw new System.ComponentModel.DataAnnotations.ValidationException($"One or more users are not workspace members. Invalid user IDs: {string.Join(", ", invalidIds)}");
        }

        return validMembers;
    }

    protected async Task<Guid> GetWorkspaceMemberId(Guid userId, CancellationToken ct = default)
    {
        var member = await UnitOfWork.Set<Domain.Entities.Relationship.WorkspaceMember>()
            .AsNoTracking()
            .FirstOrDefaultAsync(wm => wm.UserId == userId && wm.ProjectWorkspaceId == WorkspaceId, ct);

        return member?.Id ?? throw new KeyNotFoundException($"User {userId} is not a member of workspace {WorkspaceId}");
    }

    protected async Task<List<Guid>> GetWorkspaceMemberIds(List<Guid> userIds, CancellationToken ct = default)
    {
        if (userIds == null || !userIds.Any()) return new List<Guid>();

        var members = await UnitOfWork.Set<Domain.Entities.Relationship.WorkspaceMember>()
            .AsNoTracking()
            .Where(wm => userIds.Contains(wm.UserId) && wm.ProjectWorkspaceId == WorkspaceId)
            .Select(wm => wm.Id)
            .ToListAsync(ct);

        if (members.Count != userIds.Count)
        {
            throw new System.ComponentModel.DataAnnotations.ValidationException("One or more users are not members of this workspace.");
        }

        return members;
    }

    protected IQueryable<T> QueryNoTracking<T>() where T : class
    {
        return UnitOfWork.Set<T>().AsNoTracking();
    }
}
