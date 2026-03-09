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

        return new WorkspaceSecurityContextDto
        {
            WorkspaceId = request.WorkspaceId,
            CurrentRole = snapshot.Role.ToString(),
            Permissions = BuildPermissionNames(snapshot),
            IsOwned = snapshot.IsOwned,
            PermissionFlags = new WorkspacePermissionFlagsDto
            {
                CanViewHierarchy = snapshot.CanViewHierarchy,
                CanManageWorkspace = snapshot.CanManageWorkspace,
                CanManageMembers = snapshot.CanManageMembers,
                CanCreateSpace = snapshot.CanCreateSpace,
                CanUpdateWorkspace = snapshot.CanUpdateWorkspace,
                CanDeleteWorkspace = snapshot.CanDeleteWorkspace
            }
        };
    }

    private static List<string> BuildPermissionNames(WorkspacePermissionSnapshot snapshot)
    {
        var permissions = new List<string>();

        if (snapshot.CanViewHierarchy) permissions.Add("workspace.viewHierarchy");
        if (snapshot.CanManageWorkspace) permissions.Add("workspace.manage");
        if (snapshot.CanManageMembers) permissions.Add("workspace.members.manage");
        if (snapshot.CanCreateSpace) permissions.Add("workspace.space.create");
        if (snapshot.CanUpdateWorkspace) permissions.Add("workspace.update");
        if (snapshot.CanDeleteWorkspace) permissions.Add("workspace.delete");

        return permissions;
    }
}

