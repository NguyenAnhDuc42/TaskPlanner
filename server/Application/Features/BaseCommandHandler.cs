
using Application.Common.Exceptions;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain.Common.Interfaces;
using Domain.Enums;
using server.Application.Interfaces;

namespace Application.Features;

public abstract class BaseCommandHandler
{
    protected readonly IUnitOfWork UnitOfWork;
    protected readonly IPermissionService PermissionService;
    protected readonly ICurrentUserService CurrentUserService;
    protected Guid CurrentUserId => CurrentUserService.CurrentUserId();

    public BaseCommandHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService)
    {
        UnitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        PermissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        CurrentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    protected async Task RequirePermissionAsync<TEntity>(TEntity entity, PermissionAction permission, CancellationToken ct) where TEntity : IIdentifiable
    {
        if (CurrentUserId == Guid.Empty) throw new UnauthorizedAccessException();

        var hasPermission = await PermissionService.HasPermissionAsync(CurrentUserId, entity, permission, ct);

        if (!hasPermission) throw new ForbiddenAccessException();
    }

}
