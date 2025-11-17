using System;
using Application.Helper;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Common;
using Domain.Entities.ProjectEntities;
using Domain.Enums.RelationShip;
using server.Application.Interfaces;

namespace Application.Features;

public abstract class BaseQueryHandler
{

    protected readonly IUnitOfWork UnitOfWork;
    protected readonly IPermissionService PermissionService;
    protected readonly ICurrentUserService CurrentUserService;
    protected readonly WorkspaceContext WorkspaceContext;
    protected readonly CursorHelper CursorHelper;
    protected Guid WorkspaceId => WorkspaceContext.WorkspaceId;
    protected Guid CurrentUserId => CurrentUserService.CurrentUserId();
    public BaseQueryHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext,CursorHelper cursorHelper)
    {
        UnitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        PermissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        CurrentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        WorkspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
        CursorHelper = cursorHelper ?? throw new ArgumentNullException(nameof(cursorHelper));
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
