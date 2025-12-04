
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfManagement.ArchiveWorkspace;

public class ArchiveWorkspaceHandler : BaseCommandHandler, IRequestHandler<ArchiveWorkspaceCommand, Unit>
{
    public ArchiveWorkspaceHandler(
        IUnitOfWork unitOfWork,
        IPermissionService permissionService,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext)
    {
    }

    public async Task<Unit> Handle(ArchiveWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = await FindOrThrowAsync<ProjectWorkspace>(request.WorkspaceId);

        if (workspace.CreatorId != CurrentUserId)
        {
            throw new UnauthorizedAccessException("Only the workspace owner can archive/unarchive the workspace");
        }

        if (request.IsArchived)
        {
            workspace.Archive();
        }
        else
        {
            workspace.Unarchive();
        }
        
        return Unit.Value;
    }
}
