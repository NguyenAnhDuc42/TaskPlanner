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

namespace Application.Features.WorkspaceFeatures.MemberManage.UpdateMembersRole;

public class UpdateMembersHandler : BaseCommandHandler, IRequestHandler<UpdateMembersCommand, Unit>
{
    public UpdateMembersHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
    : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(UpdateMembersCommand request, CancellationToken cancellationToken)
    {
        var workspace = await UnitOfWork.Set<ProjectWorkspace>()
            .Where(w => w.Id == request.workspaceId)
            .FirstAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Workspace not found.");

        await RequirePermissionAsync(workspace, EntityType.WorkspaceMember, PermissionAction.Edit, cancellationToken);


        var userIdsToUpdate = request.members.Select(m => m.userId).ToList();
        var existingMembers = await UnitOfWork.Set<WorkspaceMember>()
             .Where(wm => wm.ProjectWorkspaceId == request.workspaceId && userIdsToUpdate.Contains(wm.UserId))
             .ToListAsync(cancellationToken);
        foreach (var member in request.members)
        {
            var existingMember = existingMembers.FirstOrDefault(wm => wm.UserId == member.userId);
            if (existingMember != null)
            {
                existingMember.UpdateMembershipDetails(member.role, member.status);
            }
        }
        return Unit.Value;
    }
}
