using Application.Features.WorkspaceFeatures.Logic;
using Application.Helpers;
using Application.Interfaces.Repositories;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfManagement.GetDetailWorkspace;

public class GetDetailWorkspaceHandler : BaseFeatureHandler, IRequestHandler<GetDetailWorkspaceQuery, WorkspaceSecurityContextDto>
{
    private readonly WorkspacePermissionLogic _workspacePermissionLogic;

    public GetDetailWorkspaceHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext,
        WorkspacePermissionLogic workspacePermissionLogic)
        : base(unitOfWork, currentUserService, workspaceContext)
    {
        _workspacePermissionLogic = workspacePermissionLogic;
    }

    public async Task<WorkspaceSecurityContextDto> Handle(GetDetailWorkspaceQuery request, CancellationToken cancellationToken)
    {
        var snapshot = await _workspacePermissionLogic.GetSnapshot(
            request.WorkspaceId,
            CurrentUserId,
            cancellationToken);

        var workspace = await UnitOfWork.Set<Domain.Entities.ProjectEntities.ProjectWorkspace>()
            .FindAsync(request.WorkspaceId, cancellationToken);

        return new WorkspaceSecurityContextDto
        {
            WorkspaceId = request.WorkspaceId,
            CurrentRole = snapshot.Role.ToString(),
            IsOwned = snapshot.IsOwned,
            Theme = workspace?.Theme ?? Domain.Enums.Workspace.Theme.Light,
            Color = workspace?.Customization.Color ?? string.Empty,
            Icon = workspace?.Customization.Icon ?? string.Empty
        };
    }
}

