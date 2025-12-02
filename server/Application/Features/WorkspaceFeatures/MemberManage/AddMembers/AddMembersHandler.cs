using System;
using System.ComponentModel.DataAnnotations;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.MemberManage.AddMembers;

public class AddMembersHandler : BaseCommandHandler, IRequestHandler<AddMembersCommand, Unit>
{
    public AddMembersHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IPermissionService permissionService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(AddMembersCommand command, CancellationToken cancellationToken)
    {
        var workspace = await UnitOfWork.Set<ProjectWorkspace>().FindAsync(command.workspaceId, cancellationToken) ?? throw new KeyNotFoundException("Workspace not found");
        cancellationToken.ThrowIfCancellationRequested();
        await RequirePermissionAsync(workspace, EntityType.WorkspaceMember, PermissionAction.Create, cancellationToken);
        var emails = command.members.Select(m => m.email).ToList();
        var users = await UnitOfWork.Set<User>().Where(u => emails.Contains(u.Email)).ToListAsync(cancellationToken);

        if (users.Count != command.members.Count) throw new ValidationException("One or more users could not be found by email.");
        var memberSpecs = command.members
            .Join(users, m => m.email, u => u.Email, (m, u) => (u.Id, m.role, m.status, m.joinMethod))
            .ToList();

        workspace.AddMembers(memberSpecs, CurrentUserId);


        return Unit.Value;
    }
}

