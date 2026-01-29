using System;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.Services.Permissions;
using Domain;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.DeleteWorkspace;

public class DeleteWorkspaceHandler : BaseFeatureHandler, IRequestHandler<DeleteWorkspaceCommand, Unit>
{
    public DeleteWorkspaceHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IPermissionService permissionService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(DeleteWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = await AuthorizeAndFetchAsync<ProjectWorkspace>(request.workspaceId, PermissionAction.Delete, cancellationToken);
        workspace.SoftDelete();

        return Unit.Value;

    }
}
