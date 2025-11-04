using System;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfMange.DeleteWorkspace;

public class DeleteWorkspaceHandler : BaseCommandHandler, IRequestHandler<DeleteWorkspaceCommand, Unit>
{
    public DeleteWorkspaceHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IPermissionService permissionService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(DeleteWorkspaceCommand request, CancellationToken cancellationToken)
    {

        var workspace = await UnitOfWork.Set<ProjectWorkspace>().FindAsync(request.workspaceId, cancellationToken) ?? throw new KeyNotFoundException("Workspace not found");
        await RequirePermissionAsync(workspace, PermissionAction.Delete, cancellationToken);
        UnitOfWork.Set<ProjectWorkspace>().Remove(workspace);

        return Unit.Value;

    }
}
