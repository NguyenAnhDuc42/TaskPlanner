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
        var workspace = await UnitOfWork.Set<ProjectWorkspace>()
            .Where(w => w.Id == request.WorkspaceId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Workspace {request.WorkspaceId} not found");

        // Check if user is a member
        var membership = await UnitOfWork.Set<WorkspaceMember>()
            .Where(wm => wm.ProjectWorkspaceId == request.WorkspaceId && wm.UserId == CurrentUserId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("You are not a member of this workspace");

        // Owner cannot leave - must transfer ownership first
        if (workspace.CreatorId == CurrentUserId)
        {
            throw new InvalidOperationException("Workspace owner cannot leave. Transfer ownership first.");
        }

        // Soft delete the membership
        await UnitOfWork.Set<WorkspaceMember>()
            .Where(wm => wm.ProjectWorkspaceId == request.WorkspaceId && wm.UserId == CurrentUserId)
            .ExecuteUpdateAsync(updates =>
                updates.SetProperty(wm => wm.DeletedAt, DateTimeOffset.UtcNow)
                       .SetProperty(wm => wm.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);

        return Unit.Value;
    }
}
