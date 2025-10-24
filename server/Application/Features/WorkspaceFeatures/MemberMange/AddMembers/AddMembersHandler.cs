using System;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain.Entities;
using Domain.Entities.Relationship;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.MemberMange.AddMembers;

public class AddMembersHandler : BaseCommandHandler, IRequestHandler<AddMembersCommand, Unit>
{
    public AddMembersHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IPermissionService permissionService)
        : base(unitOfWork, permissionService, currentUserService) { }

    public async Task<Unit> Handle(AddMembersCommand command, CancellationToken cancellationToken)
    {
        await RequirePermissionAsync(command.workspaceId, EntityType.ProjectWorkspace, PermissionAction.InviteMember, cancellationToken);
        var emails = command.members.Select(m => m.email).ToList();
        var users = await UnitOfWork.Set<User>().Where(u => emails.Contains(u.Email)).ToListAsync(cancellationToken);
        var memberSpecs = command.members
            .Join(users, m => m.email, u => u.Email, (m, u) => (u.Id, m.role, m.status, m.joinMethod))
            .ToList();

        var members = WorkspaceMember.AddBulk(memberSpecs, command.workspaceId, CurrentUserId);
        await UnitOfWork.Set<WorkspaceMember>().AddRangeAsync(members, cancellationToken);

        return Unit.Value;

    }
}
