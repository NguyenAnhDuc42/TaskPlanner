using System;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.UpdateWorkspace;

public class UpdateWorkspaceHandler : BaseCommandHandler, IRequestHandler<UpdateWorkspaceCommand, Unit>
{
    public UpdateWorkspaceHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }
    public async Task<Unit> Handle(UpdateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = await UnitOfWork.Set<ProjectWorkspace>().FindAsync(request.Id, cancellationToken) ?? throw new KeyNotFoundException("Workspace not found");
        await RequirePermissionAsync(workspace, PermissionAction.Edit, cancellationToken);

        workspace.Update(
            name: request.Name,
            description: request.Description,
            color: request.Color,
            icon: request.Icon,
            theme: request.Theme,
            variant: request.Variant,
            strictJoin: request.StrictJoin,
            isArchived: request.IsArchived,
            regenerateJoinCode: request.RegenerateJoinCode
        );

        UnitOfWork.Set<ProjectWorkspace>().Update(workspace);
        return Unit.Value;
    }

}
