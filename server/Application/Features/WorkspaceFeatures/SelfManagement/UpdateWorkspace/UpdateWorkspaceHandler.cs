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
        var workspace = await FindOrThrowAsync<ProjectWorkspace>(request.Id);
        await RequirePermissionAsync(workspace, PermissionAction.Edit, cancellationToken);

        // Update basic info
        if (request.Name is not null || request.Description is not null)
            workspace.UpdateBasicInfo(request.Name, request.Description);

        // Update customization
        if (request.Color is not null || request.Icon is not null)
            workspace.UpdateCustomization(request.Color, request.Icon);

        // Update settings
        if (request.Theme.HasValue)
            workspace.UpdateTheme(request.Theme.Value);

        if (request.Variant.HasValue)
            workspace.UpdateVariant(request.Variant.Value);

        if (request.StrictJoin.HasValue)
            workspace.UpdateStrictJoin(request.StrictJoin.Value);

        // Handle archive/unarchive
        if (request.IsArchived.HasValue)
        {
            if (request.IsArchived.Value) workspace.Archive();
            else workspace.Unarchive();
        }

        // Regenerate join code if requested
        if (request.RegenerateJoinCode)
            workspace.RegenerateJoinCode();

        return Unit.Value;
    }

}
