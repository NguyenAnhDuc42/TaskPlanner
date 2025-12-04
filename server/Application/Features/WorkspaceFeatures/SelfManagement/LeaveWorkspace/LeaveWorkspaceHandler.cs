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

namespace Application.Features.WorkspaceFeatures.SelfManagement.LeaveWorkspace;

public class LeaveWorkspaceHandler : BaseCommandHandler, IRequestHandler<LeaveWorkspaceCommand, Unit>
{
    public LeaveWorkspaceHandler(
        IUnitOfWork unitOfWork,
        IPermissionService permissionService,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext)
    {
    }

    public async Task<Unit> Handle(LeaveWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = await FindOrThrowAsync<ProjectWorkspace>(request.WorkspaceId);

        // Check if user is a member
        var isMember = await UnitOfWork.Set<WorkspaceMember>()
            .AnyAsync(wm => wm.ProjectWorkspaceId == request.WorkspaceId && wm.UserId == CurrentUserId, cancellationToken);
            
        if (!isMember)
            throw new ValidationException("You are not a member of this workspace");

        // Owner cannot leave - must transfer ownership first
        if (workspace.CreatorId == CurrentUserId)
        {
            throw new ValidationException("Workspace owner cannot leave. Transfer ownership first.");
        }

        // Load members to ensure the entity can process removal
        await UnitOfWork.Set<WorkspaceMember>()
            .Where(wm => wm.ProjectWorkspaceId == request.WorkspaceId && wm.UserId == CurrentUserId)
            .LoadAsync(cancellationToken);

        workspace.RemoveMembers(new[] { CurrentUserId });

        return Unit.Value;
    }
}
