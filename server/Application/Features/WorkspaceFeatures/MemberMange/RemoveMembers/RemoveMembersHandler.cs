using System;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.MemberMange.RemoveMembers;

public class RemoveMembersHandler : BaseCommandHandler, IRequestHandler<RemoveMembersCommand, Unit>
{
    public RemoveMembersHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
    : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(RemoveMembersCommand request, CancellationToken cancellationToken)
    {
        var workspace = await UnitOfWork.Set<ProjectWorkspace>().FindAsync(request.workspaceId, cancellationToken) ?? throw new KeyNotFoundException("Workspace not found");

        await RequirePermissionAsync(workspace, EntityType.WorkspaceMember, PermissionAction.Delete, cancellationToken);
        var member = await UnitOfWork.Set<WorkspaceMember>().Where(wm => wm.ProjectWorkspaceId == request.workspaceId && request.memberIds.Contains(wm.UserId)).ToListAsync(cancellationToken);
        if (member == null) throw new KeyNotFoundException("No members found to remove");
        UnitOfWork.Set<WorkspaceMember>().RemoveRange(member);
        return Unit.Value;
    }
}
