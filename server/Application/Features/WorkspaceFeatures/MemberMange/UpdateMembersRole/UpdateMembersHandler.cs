using System;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain.Entities.Relationship;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.MemberMange.UpdateMembersRole;

public class UpdateMembersHandler : BaseCommandHandler, IRequestHandler<UpdateMembersCommand, Unit>
{
    public UpdateMembersHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService)
        : base(unitOfWork, permissionService, currentUserService) { }
    public async Task<Unit> Handle(UpdateMembersCommand request, CancellationToken cancellationToken)
    {
        await RequirePermissionAsync(request.workspaceId, Domain.Enums.EntityType.ProjectWorkspace, Domain.Enums.PermissionAction.UpdateMember, cancellationToken);


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
