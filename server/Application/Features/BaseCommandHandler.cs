
using Application.Common.Exceptions;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Common;
using Domain.Common.Interfaces;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using Domain.Enums.RelationShip;
using server.Application.Interfaces;

namespace Application.Features;

public abstract class BaseCommandHandler
{
    protected readonly IUnitOfWork UnitOfWork;
    protected readonly IPermissionService PermissionService;
    protected readonly ICurrentUserService CurrentUserService;
    protected readonly WorkspaceContext WorkspaceContext;
    protected Guid WorkspaceId => WorkspaceContext.WorkspaceId;
    protected Guid CurrentUserId => CurrentUserService.CurrentUserId();

    public BaseCommandHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
    {
        UnitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        PermissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        CurrentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        WorkspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
    }

    protected async Task RequirePermissionAsync<TEntity>(
    TEntity entity,
    PermissionAction permission,
    CancellationToken ct)
    where TEntity : Entity
    {
        if (CurrentUserId == Guid.Empty)
            throw new UnauthorizedAccessException();

        var hasPermission = await PermissionService.CanPerformAsync(
            WorkspaceId,
            CurrentUserId,
            entity,
            permission,
            ct);

        if (!hasPermission)
            throw new ForbiddenAccessException();
    }

    // Variant 2: Create child in parent
    protected async Task RequirePermissionAsync<TParent>(
        TParent parentEntity,
        EntityType childType,
        PermissionAction permission,
        CancellationToken ct)
        where TParent : Entity
    {
        if (CurrentUserId == Guid.Empty)
            throw new UnauthorizedAccessException();

        var hasPermission = await PermissionService.CanPerformAsync(
            WorkspaceId,
            CurrentUserId,
            parentEntity,
            childType,
            permission,
            ct);

        if (!hasPermission)
            throw new ForbiddenAccessException();
    }

    // Variant 3: Action on child with parent
    protected async Task RequirePermissionAsync<TChild, TParent>(
        TChild childEntity,
        TParent parentEntity,
        PermissionAction permission,
        CancellationToken ct)
        where TChild : Entity
        where TParent : Entity
    {
        if (CurrentUserId == Guid.Empty)
            throw new UnauthorizedAccessException();

        var hasPermission = await PermissionService.CanPerformAsync(
            WorkspaceId,
            CurrentUserId,
            childEntity,
            parentEntity,
            permission,
            ct);

        if (!hasPermission)
            throw new ForbiddenAccessException();
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

    private async Task<Entity> FindOrThrowAsync<T>(Guid id)
    where T : Entity
    {
        var entity = await UnitOfWork.Set<T>()
            .FindAsync(new object?[] { id });

        return entity ?? throw new KeyNotFoundException($"Layer not found: {typeof(T).Name}:{id}");
    }

}
