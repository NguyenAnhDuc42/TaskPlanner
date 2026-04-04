
using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.Relationship;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
            
        if (workspace == null) throw new KeyNotFoundException($"Workspace {request.WorkspaceId} not found");

        var member = await UnitOfWork.Set<WorkspaceMember>()
            .AsNoTracking()
            .FirstOrDefaultAsync(wm => wm.ProjectWorkspaceId == request.WorkspaceId && wm.UserId == CurrentUserId, cancellationToken);

        var role = member?.Role ?? Role.Guest;

        return new WorkspaceSecurityContextDto
        {
            WorkspaceId = request.WorkspaceId,
            CurrentRole = role.ToString(),
            IsOwned = role == Role.Owner,
            Theme = workspace.Theme,
            Color = workspace.Customization.Color,
            Icon = workspace.Customization.Icon,
            
            // Permissions mapping
            CanEdit = role == Role.Owner || role == Role.Admin,
            CanInvite = role == Role.Owner || role == Role.Admin,
            CanManageMembers = role == Role.Owner || role == Role.Admin,
            
            // Feature Toggles (Frontend control)
            IsDashboardEnabled = false // Disabled per user request
        };
    }
}
