using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Interfaces.Services.Permissions;
using MediatR;
using server.Application.Interfaces;

public class PermissionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : ICommand<TResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;

    public PermissionBehavior(
        ICurrentUserService currentUserService,
        IPermissionService permissionService)
    {
        _currentUserService = currentUserService;
        _permissionService = permissionService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Check if command implements authorization interface
        if (request is IRequirePermission permissionRequest)
        {
            var userId = _currentUserService.CurrentUserId();
            if (userId == Guid.Empty)
                throw new UnauthorizedAccessException();

            var hasPermission = await _permissionService.HasPermissionAsync(
                userId,
                permissionRequest.EntityId,
                permissionRequest.EntityType,
                permissionRequest.RequiredPermission,
                cancellationToken);

            if (!hasPermission)
                throw new ForbiddenAccessException();
        }

        return await next();
    }
}
