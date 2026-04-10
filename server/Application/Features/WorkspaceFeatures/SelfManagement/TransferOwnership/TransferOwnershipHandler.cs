using System.ComponentModel.DataAnnotations;
using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features;
using Application.Interfaces;
using Application.Interfaces.Data;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.WorkspaceFeatures.SelfManagement.TransferOwnership;

public class TransferOwnershipHandler : ICommandHandler<TransferOwnershipCommand>
{
    private readonly IDataBase _db;
    private readonly ICurrentUserService _currentUserService;

    public TransferOwnershipHandler(IDataBase db, ICurrentUserService currentUserService) {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(TransferOwnershipCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) return Result.Failure(Error.Unauthorized("User.NotAuthenticated", "User not authenticated."));

        var workspace = await _db.Workspaces.FindAsync(new object[] { request.WorkspaceId }, cancellationToken);
        if (workspace == null) return Result.Failure(Error.NotFound("Workspace.NotFound", $"Workspace {request.WorkspaceId} not found"));

        // Extra check: Only current owner can transfer ownership (Role.Owner)
        if (workspace.CreatorId != currentUserId) return Result.Failure(Error.Forbidden("Workspace.Forbidden", "Only the workspace owner can transfer ownership"));

        // Cannot transfer to self
        if (request.NewOwnerId == currentUserId) return Result.Failure(Error.Validation("Workspace.TransferSameUser", "Cannot transfer ownership to yourself"));

        // Load members for tracking
        await _db.Members.Where(wm => wm.ProjectWorkspaceId == request.WorkspaceId).LoadAsync(cancellationToken);

        try 
        {
            workspace.TransferOwnership(request.NewOwnerId);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.ConditionNotMet with { Description = ex.Message });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
