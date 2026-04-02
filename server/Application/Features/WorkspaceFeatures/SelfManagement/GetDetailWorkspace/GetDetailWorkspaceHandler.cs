
using Application.Helpers;
using Application.Interfaces.Repositories;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfManagement.GetDetailWorkspace;

public class GetDetailWorkspaceHandler : BaseFeatureHandler, IRequestHandler<GetDetailWorkspaceQuery, WorkspaceSecurityContextDto>
{

    public GetDetailWorkspaceHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext)
    {

    }

    public async Task<WorkspaceSecurityContextDto> Handle(GetDetailWorkspaceQuery request, CancellationToken cancellationToken)
    {
        var workspace = await UnitOfWork.Set<Domain.Entities.ProjectEntities.ProjectWorkspace>()
            .FindAsync(request.WorkspaceId, cancellationToken);

        return new WorkspaceSecurityContextDto
        {
            WorkspaceId = request.WorkspaceId,
            CurrentRole = Domain.Enums.Role.Owner.ToString(),
            IsOwned = true,
            Theme = workspace?.Theme ?? Domain.Enums.Workspace.Theme.Light,
            Color = workspace?.Customization.Color ?? string.Empty,
            Icon = workspace?.Customization.Icon ?? string.Empty
        };
    }
}

