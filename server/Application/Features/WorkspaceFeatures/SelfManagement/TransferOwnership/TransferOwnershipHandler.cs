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
        var workspace = await UnitOfWork.Set<ProjectWorkspace>()
            .Where(w => w.Id == request.WorkspaceId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Workspace {request.WorkspaceId} not found");

        // Only current owner can transfer ownership
        if (workspace.CreatorId != CurrentUserId)
        {
            throw new UnauthorizedAccessException("Only the workspace owner can transfer ownership");
        }

        // Cannot transfer to self
        if (request.NewOwnerId == CurrentUserId)
        {
            throw new InvalidOperationException("Cannot transfer ownership to yourself");
        }

        // Check if new owner is a member
        var newOwnerMembership = await UnitOfWork.Set<WorkspaceMember>()
            .Where(wm => wm.ProjectWorkspaceId == request.WorkspaceId 
                      && wm.UserId == request.NewOwnerId
                      && wm.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("New owner must be an existing workspace member");

        // Update workspace creator
        await UnitOfWork.Set<ProjectWorkspace>()
            .Where(w => w.Id == request.WorkspaceId)
            .ExecuteUpdateAsync(updates =>
                updates.SetProperty(w => w.CreatorId, request.NewOwnerId)
                       .SetProperty(w => w.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);

        // Update new owner's role to Owner
        newOwnerMembership.UpdateMembershipDetails(Role.Owner, newOwnerMembership.Status);

        // Downgrade previous owner to Admin
        var previousOwnerMembership = await UnitOfWork.Set<WorkspaceMember>()
            .Where(wm => wm.ProjectWorkspaceId == request.WorkspaceId 
                      && wm.UserId == CurrentUserId
                      && wm.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (previousOwnerMembership != null)
        {
            previousOwnerMembership.UpdateMembershipDetails(Role.Admin, previousOwnerMembership.Status);
        }

        return Unit.Value;
    }
}
