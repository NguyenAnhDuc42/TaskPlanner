
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using server.Application.Interfaces;

namespace Application.Features;

public abstract class BaseCommandHandler : BaseFeatureHandler
{
    protected BaseCommandHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext)
    {
    }
}
