using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Application.Interfaces;
using Application.Features;

namespace Application.Features.WorkspaceFeatures.SelfManagement.JoinWorkspaceByCode;

public class JoinWorkspaceByCodeHandler(
    IDataBase db, 
    ICurrentUserService currentUserService, 
    HybridCache cache, 
    IRealtimeService realtime
) : ICommandHandler<JoinWorkspaceByCodeCommand, JoinWorkspaceByCodeResult>
{
    public async Task<Result<JoinWorkspaceByCodeResult>> Handle(JoinWorkspaceByCodeCommand request, CancellationToken ct)
    {
        var currentUserId = currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) 
            return Result<JoinWorkspaceByCodeResult>.Failure(Error.Unauthorized("User.NotAuthenticated", "User not authenticated."));

        var normalizedCode = request.JoinCode.Trim().ToUpperInvariant();
        var workspace = await db.Workspaces
            .ByJoinCode(normalizedCode)
            .WhereNotDeleted()
            .FirstOrDefaultAsync(ct);
            
        if (workspace == null) return Result<JoinWorkspaceByCodeResult>.Failure(Error.Validation("Workspace.InvalidJoinCode", "Invalid join code."));
        if (workspace.IsArchived) return Result<JoinWorkspaceByCodeResult>.Failure(Error.Validation("Workspace.Archived", "Cannot join an archived workspace."));

        var existingMember = await db.WorkspaceMembers.IgnoreQueryFilters()
            .ByWorkspace(workspace.Id)
            .ByUser(currentUserId)
            .FirstOrDefaultAsync(ct);

        JoinWorkspaceByCodeResult dataResult;
        if (existingMember is null)
        {
            var status = workspace.StrictJoin ? MembershipStatus.Pending : MembershipStatus.Active;
            workspace.AddMember(currentUserId, Role.Member, currentUserId, "Code");
            dataResult = new JoinWorkspaceByCodeResult(workspace.Id, status.ToString(), true);
        }
        else if (existingMember.DeletedAt != null)
        {
            existingMember.RestoreForJoinByCode(workspace.StrictJoin);
            dataResult = new JoinWorkspaceByCodeResult(workspace.Id, existingMember.Status.ToString(), false);
        }
        else
        {
            existingMember.JoinByCode(workspace.StrictJoin);
            dataResult = new JoinWorkspaceByCodeResult(workspace.Id, existingMember.Status.ToString(), false);
        }

        await db.SaveChangesAsync(ct);

        await cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(currentUserId), ct);
        
        _ = realtime.NotifyUserAsync(currentUserId, "WorkspaceJoined", new { WorkspaceId = workspace.Id }, ct);

        return Result<JoinWorkspaceByCodeResult>.Success(dataResult);
    }
}
