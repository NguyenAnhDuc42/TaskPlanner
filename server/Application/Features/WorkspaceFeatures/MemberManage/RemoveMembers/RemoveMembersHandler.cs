using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Application;

public class RemoveMembersHandler(
    TaskPlanDbContext db,
    WorkspaceContext context,
    RealtimeService realtimeService,
    PermissionService permissionService,
    HybridCache cache,
    ILogger<RemoveMembersHandler> logger
) : ICommandHandler<RemoveMembersCommand>
{
    public async Task<Result> Handle(RemoveMembersCommand request, CancellationToken cancellationToken)
    {
        var hasAccess = await permissionService.VerifyAsync(Role.Admin, cancellationToken: cancellationToken);
        if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);

        if (request.MemberIds.Count == 0) return Result.Success();

        // Self-removal is not allowed via this endpoint — use Leave Workspace instead
        if (request.MemberIds.Contains(context.CurrentMember.Id))
            return Result.Failure(Error.Validation("Member.CannotRemoveSelf", "You cannot remove yourself. Use Leave Workspace to exit."));

        // Cannot remove a member whose role is >= yours (protects peers and the Owner)
        var callerRole = context.CurrentMember.Role;
        var targetRoles = await db.WorkspaceMembers
            .Where(wm => request.MemberIds.Contains(wm.Id) && wm.DeletedAt == null)
            .Select(wm => wm.Role)
            .ToListAsync(cancellationToken);

        if (targetRoles.Any(r => r >= callerRole))
            return Result.Failure(Error.Forbidden("Member.CannotRemovePeerOrSuperior", "You can only remove members with a lower role than your own."));

        var removedUserIds = await db.WorkspaceMembers
            .Where(wm => request.MemberIds.Contains(wm.Id) && wm.DeletedAt == null)
            .Select(wm => wm.UserId)
            .ToListAsync(cancellationToken);

        var affected = await db.WorkspaceMembers
            .Where(wm => request.MemberIds.Contains(wm.Id) && wm.DeletedAt == null)
            .ExecuteUpdateAsync(u => u
                .SetProperty(wm => wm.DeletedAt, DateTimeOffset.UtcNow)
                .SetProperty(wm => wm.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken);

        if (affected > 0)
        {
            await cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceMembersTag(request.WorkspaceId), cancellationToken);
            foreach (var userId in removedUserIds)
            {
                await cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(userId), cancellationToken);
            }

            _ = realtimeService
                .NotifyEntitiesDeletedAsync(
                    request.WorkspaceId,
                    new EntityBatchDelete { MemberIds = request.MemberIds },
                    default)
                .ContinueWith(
                    t => logger.LogError(t.Exception, "Failed to send realtime notification for RemoveMembers in workspace {WorkspaceId}", request.WorkspaceId),
                    TaskContinuationOptions.OnlyOnFaulted);
        }

        return Result.Success();
    }
}
