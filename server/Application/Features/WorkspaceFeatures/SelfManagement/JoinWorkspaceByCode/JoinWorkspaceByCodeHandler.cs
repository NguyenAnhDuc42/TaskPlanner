using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
namespace Application;

public class JoinWorkspaceByCodeHandler(
    TaskPlanDbContext db,
    CurrentUserService currentUserService,
    HybridCache cache,
    RealtimeService realtime
) : ICommandHandler<JoinWorkspaceByCodeCommand, JoinWorkspaceByCodeResult>
{
    public async Task<Result<JoinWorkspaceByCodeResult>> Handle(JoinWorkspaceByCodeCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty)
            return Result<JoinWorkspaceByCodeResult>.Failure(Error.Unauthorized("User.NotAuthenticated", "User not authenticated."));

        var normalizedCode = request.JoinCode.Trim().ToUpperInvariant();
        var workspace = await db.ProjectWorkspaces
            .FirstOrDefaultAsync(w => w.JoinCode == normalizedCode && w.DeletedAt == null, cancellationToken);

        if (workspace == null) return Result<JoinWorkspaceByCodeResult>.Failure(Error.Validation("Workspace.InvalidJoinCode", "Invalid join code."));
        if (workspace.IsArchived) return Result<JoinWorkspaceByCodeResult>.Failure(Error.Validation("Workspace.Archived", "Cannot join an archived workspace."));

        var existingMember = await db.WorkspaceMembers
        .FirstOrDefaultAsync(m => m.UserId == currentUserId && m.DeletedAt == null, cancellationToken);

        JoinWorkspaceByCodeResult dataResult;
        if (existingMember is null)
        {
            workspace.AddMemberByCode(currentUserId, currentUserId);
            var status = workspace.StrictJoin ? MembershipStatus.Pending : MembershipStatus.Active;
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

        await db.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(currentUserId), cancellationToken);

        _ = realtime.NotifyUserAsync(currentUserId, "WorkspaceJoined", new { WorkspaceId = workspace.Id }, cancellationToken);

        return Result<JoinWorkspaceByCodeResult>.Success(dataResult);
    }
}



