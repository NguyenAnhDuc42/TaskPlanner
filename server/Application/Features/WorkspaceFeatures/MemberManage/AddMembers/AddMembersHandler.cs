using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Application;

public class AddMembersHandler(
    TaskPlanDbContext db,
    WorkspaceContext context,
    PermissionService permissionService,
    RealtimeService realtimeService,
    HybridCache cache,
    ILogger<AddMembersHandler> logger
) : ICommandHandler<AddMembersCommand>
{
    public async Task<Result> Handle(AddMembersCommand request, CancellationToken cancellationToken)
    {
        var hasAccess = await permissionService.VerifyAsync(Role.Admin, cancellationToken: cancellationToken);
        if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);

        var workspace = await db.ProjectWorkspaces
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && w.DeletedAt == null, cancellationToken);
        if (workspace == null) return Result.Failure(WorkspaceError.NotFound);

        if (request.Members.Count == 0) return Result.Success();

        // Cannot assign a role higher than your own
        var callerRole = context.CurrentMember.Role;
        if (request.Members.Any(m => !callerRole.IsAtLeast(m.Role)))
            return Result.Failure(Error.Forbidden("Member.RoleEscalation", "You cannot assign a role higher than your own."));

        var lowerEmails = request.Members.Select(m => m.Email.ToLower()).ToList();

        // Happy path: match only users that exist — unknown emails are silently skipped
        var users = await db.Users
            .Where(u => lowerEmails.Contains(u.Email.ToLower()) && u.DeletedAt == null)
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
            return Result.Failure(Error.NotFound("User.NoneFound", "None of the provided email addresses belong to registered users."));

        var existingUserIds = await db.WorkspaceMembers
            .Where(wm => wm.ProjectWorkspaceId == workspace.Id && wm.DeletedAt == null)
            .Select(wm => wm.UserId)
            .ToHashSetAsync(cancellationToken);

        var memberRoleByEmail = request.Members.ToDictionary(m => m.Email, m => m.Role, StringComparer.OrdinalIgnoreCase);

        // Skip users that are already members
        var newMembers = users
            .Where(u => !existingUserIds.Contains(u.Id))
            .Select(u => new WorkspaceMember(
                userId: u.Id,
                projectWorkspaceId: workspace.Id,
                role: memberRoleByEmail[u.Email],
                status: MembershipStatus.Active,
                creatorId: context.CurrentMember.Id,
                joinMethod: "Invite"))
            .ToList();

        if (newMembers.Count == 0)
            return Result.Failure(Error.Conflict("Member.AlreadyExists", "All specified users are already members of this workspace."));

        await db.WorkspaceMembers.AddRangeAsync(newMembers, cancellationToken);
        var affected = await db.SaveChangesAsync(cancellationToken);

        if (affected > 0)
        {
            await cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceMembersTag(request.WorkspaceId), cancellationToken);

            var userLookup = users.ToDictionary(u => u.Id);
            var records = newMembers
                .Select(wm => MemberRecord.FromDomain(wm, userLookup[wm.UserId]))
                .ToList();

            _ = realtimeService
                .NotifyEntitiesUpdatedAsync(
                    request.WorkspaceId,
                    new EntityBatchUpdate { Members = records },
                    default)
                .ContinueWith(
                    t => logger.LogError(t.Exception, "Failed to send realtime notification for AddMembers in workspace {WorkspaceId}", request.WorkspaceId),
                    TaskContinuationOptions.OnlyOnFaulted);
        }

        return Result.Success();
    }
}
