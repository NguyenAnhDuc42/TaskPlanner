using System;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.Services.Permissions;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfMange.DeleteWorkspace;

public class DeleteWorkspaceHandler : BaseCommandHandler, IRequestHandler<DeleteWorkspaceCommand, Unit>
{
    public DeleteWorkspaceHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IPermissionService permissionService)
        : base(unitOfWork, permissionService, currentUserService) { }

    public async Task<Unit> Handle(DeleteWorkspaceCommand request, CancellationToken cancellationToken)
    {

        var workspace = await UnitOfWork.Set<ProjectWorkspace>().FindAsync(request.workspaceId);
        if (workspace == null) throw new KeyNotFoundException("Workspace not found");
        await RequirePermissionAsync(request.workspaceId, EntityType.ProjectWorkspace, PermissionAction.Delete, cancellationToken);
        UnitOfWork.Set<ProjectWorkspace>().Remove(workspace);

        return Unit.Value;

    }
}
