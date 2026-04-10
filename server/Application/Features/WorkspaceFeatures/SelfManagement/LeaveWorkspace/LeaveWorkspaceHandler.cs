using System.ComponentModel.DataAnnotations;
using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features;
using Application.Interfaces;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.WorkspaceFeatures.SelfManagement.LeaveWorkspace;

public class LeaveWorkspaceHandler : ICommandHandler<LeaveWorkspaceCommand>
{
    private readonly IDataBase _db;
    private readonly ICurrentUserService _currentUserService;

    public LeaveWorkspaceHandler(IDataBase db, ICurrentUserService currentUserService) {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(LeaveWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) return Result.Failure(Error.Unauthorized("User.NotAuthenticated", "User not authenticated."));

        var workspace = await _db.Workspaces.FindAsync(new object[] { request.WorkspaceId }, cancellationToken);
        if (workspace == null) return Result.Failure(Error.NotFound("Workspace.NotFound", $"Workspace {request.WorkspaceId} not found"));

        // Check if user is a member
        var isMember = await _db.Members
            .ByWorkspace(request.WorkspaceId)
            .ByUser(currentUserId)
            .AnyAsync(cancellationToken);
            
        if (!isMember) return Result.Failure(Error.Validation("Workspace.NotMember", "You are not a member of this workspace"));

        // Owner cannot leave - must transfer ownership first
        if (workspace.CreatorId == currentUserId) return Result.Failure(Error.Validation("Workspace.OwnerCannotLeave", "Workspace owner cannot leave. Transfer ownership first."));

        // Load members to ensure the entity can process removal
        await _db.Members
            .ByWorkspace(request.WorkspaceId)
            .ByUser(currentUserId)
            .LoadAsync(cancellationToken);

        workspace.RemoveMembers(new[] { currentUserId });

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
