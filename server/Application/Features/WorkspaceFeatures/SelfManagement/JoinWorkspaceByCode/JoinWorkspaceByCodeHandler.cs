using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using server.Application.Interfaces;

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
            return Result.Failure<JoinWorkspaceByCodeResult>(Error.Unauthorized("User.NotAuthenticated", "User not authenticated."));

        var normalizedCode = request.JoinCode.Trim().ToUpperInvariant();
        var workspace = await db.Workspaces
            .ByJoinCode(normalizedCode)
            .WhereNotDeleted()
            .FirstOrDefaultAsync(ct);
            
        if (workspace == null) return Result.Failure<JoinWorkspaceByCodeResult>(Error.Validation("Workspace.InvalidJoinCode", "Invalid join code."));
        if (workspace.IsArchived) return Result.Failure<JoinWorkspaceByCodeResult>(Error.Validation("Workspace.Archived", "Cannot join an archived workspace."));

        var existingMember = await db.WorkspaceMembers.IgnoreQueryFilters()
            .ByWorkspace(workspace.Id)
            .ByUser(currentUserId)
            .FirstOrDefaultAsync(ct);

        JoinWorkspaceByCodeResult dataResult;
        if (existingMember is null)
        {
            var status = workspace.StrictJoin ? MembershipStatus.Pending : MembershipStatus.Active;
            var member = WorkspaceMember.Create(currentUserId, workspace.Id, Role.Member, status, currentUserId, "Code");
            workspace.AddMember(member, currentUserId);
            dataResult = new JoinWorkspaceByCodeResult(workspace.Id, status.ToString(), true);
        }
        else if (existingMember.DeletedAt != null)
        {
            existingMember.RestoreForJoinByCode(workspace.StrictJoin);
            dataResult = new JoinWorkspaceByCodeResult(workspace.Id, existingMember.MembershipStatus.ToString(), false);
        }
        else
        {
            existingMember.JoinByCode(workspace.StrictJoin);
            dataResult = new JoinWorkspaceByCodeResult(workspace.Id, existingMember.MembershipStatus.ToString(), false);
        }

        await db.SaveChangesAsync(ct);

        await cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(currentUserId), ct);
        
        _ = realtime.NotifyUserAsync(currentUserId, "WorkspaceJoined", new { WorkspaceId = workspace.Id }, ct);

        return Result.Success(dataResult);
    }
}
