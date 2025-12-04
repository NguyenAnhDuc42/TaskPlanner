using System.ComponentModel.DataAnnotations;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfManagement.TransferOwnership;

public class TransferOwnershipHandler : BaseCommandHandler, IRequestHandler<TransferOwnershipCommand, Unit>
{
    public TransferOwnershipHandler(
        IUnitOfWork unitOfWork,
        IPermissionService permissionService,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext)
    {
    }

    public async Task<Unit> Handle(TransferOwnershipCommand request, CancellationToken cancellationToken)
    {
        var workspace = await FindOrThrowAsync<ProjectWorkspace>(request.WorkspaceId);

        // Only current owner can transfer ownership
        if (workspace.CreatorId != CurrentUserId)
        {
            throw new UnauthorizedAccessException("Only the workspace owner can transfer ownership");
        }

        // Cannot transfer to self
        if (request.NewOwnerId == CurrentUserId)
        {
            throw new ValidationException("Cannot transfer ownership to yourself");
        }

        // Perform transfer via domain entity
        // Note: We need to ensure members are loaded for this to work correctly if they aren't already.
        // Since FindOrThrowAsync might not include members, we should verify or load them.
        // However, for now assuming EF Core tracking or explicit loading if needed. 
        // Ideally FindOrThrowAsync should include members or we load them explicitly.
        
        // Let's explicitly load members to be safe as the entity method relies on them
        await UnitOfWork.Set<WorkspaceMember>()
            .Where(wm => wm.ProjectWorkspaceId == request.WorkspaceId)
            .LoadAsync(cancellationToken);

        try 
        {
            workspace.TransferOwnership(request.NewOwnerId);
        }
        catch (InvalidOperationException ex)
        {
            throw new ValidationException(ex.Message);
        }

        return Unit.Value;
    }
}
