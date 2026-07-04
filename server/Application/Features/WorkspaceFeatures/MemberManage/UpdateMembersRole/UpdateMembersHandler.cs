using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Application;

public class UpdateMembersHandler(
    TaskPlanDbContext db,
    WorkspaceContext context,
    PermissionService permissionService,
    RealtimeService realtimeService,
    HybridCache cache,
    ILogger<UpdateMembersHandler> logger
) : ICommandHandler<UpdateMembersCommand>
{
    public async Task<Result> Handle(UpdateMembersCommand request, CancellationToken cancellationToken)
    {
        var hasAccess = await permissionService.VerifyAsync(Role.Admin, cancellationToken: cancellationToken);
        if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);

        var callerRole = context.CurrentMember.Role;
        var memberIds = request.Members.Select(m => m.MemberId).ToHashSet();
        var lookup = request.Members.ToDictionary(m => m.MemberId);

        var workspaceMembers = await db.WorkspaceMembers
            .Include(wm => wm.User)
            .Where(wm => memberIds.Contains(wm.Id) && wm.DeletedAt == null)
            .ToListAsync(cancellationToken);

        // Cannot modify a member whose role is >= yours (protects peers and superiors)
        if (workspaceMembers.Any(wm => !callerRole.IsAtLeast(wm.Role) || wm.Role >= callerRole))
            return Result.Failure(Error.Forbidden("Member.CannotModifyPeerOrSuperior", "You can only modify members with a lower role than your own."));

        // Cannot promote someone to a role higher than yours
        var newRoles = lookup.Values.Where(v => v.Role.HasValue).Select(v => v.Role!.Value);
        if (newRoles.Any(r => !callerRole.IsAtLeast(r)))
            return Result.Failure(Error.Forbidden("Member.RoleEscalation", "You cannot assign a role higher than your own."));

        foreach (var wm in workspaceMembers)
        {
            var update = lookup[wm.Id];
            // Use ApproveMembership when activating so JoinedAt is set correctly
            if (update.Status == MembershipStatus.Active && wm.Status != MembershipStatus.Active)
                wm.ApproveMembership();
            else
                wm.Update(update.Role, update.Status);
        }

        var affected = await db.SaveChangesAsync(cancellationToken);

        if (affected > 0)
        {
            await cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceMembersTag(request.WorkspaceId), cancellationToken);
            foreach (var wm in workspaceMembers)
            {
                await cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(wm.UserId), cancellationToken);
            }

            var records = workspaceMembers.Select(wm => MemberRecord.FromDomain(wm, wm.User)).ToList();

            _ = realtimeService
                .NotifyEntitiesUpdatedAsync(
                    request.WorkspaceId,
                    new EntityBatchUpdate { Members = records },
                    default)
                .ContinueWith(
                    t => logger.LogError(t.Exception, "Failed to send realtime notification for UpdateMembers in workspace {WorkspaceId}", request.WorkspaceId),
                    TaskContinuationOptions.OnlyOnFaulted);
        }

        return Result.Success();
    }
}
