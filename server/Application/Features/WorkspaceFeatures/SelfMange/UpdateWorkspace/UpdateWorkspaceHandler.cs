using System;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfMange.UpdateWorkspace;

public class UpdateWorkspaceHandler : BaseCommandHandler, IRequestHandler<UpdateWorkspaceCommand, Unit>
{
    public UpdateWorkspaceHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService)
        : base(unitOfWork, permissionService, currentUserService) { }
    public async Task<Unit> Handle(UpdateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = await UnitOfWork.Set<ProjectWorkspace>().FindAsync(request.Id, cancellationToken) ?? throw new KeyNotFoundException("Workspace not found");
        await RequirePermissionAsync(request.Id, Domain.Enums.EntityType.ProjectWorkspace,PermissionAction.Edit, cancellationToken);

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
