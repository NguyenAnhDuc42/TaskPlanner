using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;
using Application.Helpers;
using Application.Features.WorkspaceFeatures.Logic;

namespace Application.Features.WorkspaceFeatures.MemberManage.RemoveMembers;

public class RemoveMembersHandler : BaseFeatureHandler, IRequestHandler<RemoveMembersCommand, Guid>
{
    private readonly WorkspacePermissionLogic _workspacePermissionLogic;

    public RemoveMembersHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext,
        WorkspacePermissionLogic workspacePermissionLogic)
        : base(unitOfWork, currentUserService, workspaceContext)
    {
        _workspacePermissionLogic = workspacePermissionLogic;
    }

    public async Task<Guid> Handle(RemoveMembersCommand request, CancellationToken cancellationToken)
    {
        await _workspacePermissionLogic.EnsureCanManageMembers(request.workspaceId, CurrentUserId, cancellationToken);

        var workspace = await FindOrThrowAsync<ProjectWorkspace>(request.workspaceId);

        var members = await UnitOfWork.Set<WorkspaceMember>()
            .Where(wm => wm.ProjectWorkspaceId == request.workspaceId && request.memberIds.Contains(wm.UserId))
            .ToListAsync(cancellationToken);

        if (members.Any())
        {
            workspace.RemoveMembers(members.Select(m => m.UserId));
            
            // Note: ProjectWorkspace.RemoveMembers raises WorkspaceMemberRemovedEvent
            // which handles the cache/SignalR plumbing.
        }

        return workspace.Id;
    }
}
