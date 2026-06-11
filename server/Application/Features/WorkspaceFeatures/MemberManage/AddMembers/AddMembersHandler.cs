using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Caching.Hybrid;

namespace Application;

public class AddMembersHandler(TaskPlanDbContext db, WorkspaceContext context,PermissionService permissionService,RealtimeService realtimeService, HybridCache cache) : ICommandHandler<AddMembersCommand>
{
    public async Task<Result> Handle(AddMembersCommand request, CancellationToken cancellationToken)
    {
        var hasAccess = await permissionService.VerifyAsync(Role.Admin,cancellationToken: cancellationToken);
        if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);

        var workspace = await db.ProjectWorkspaces.FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && w.DeletedAt == null,cancellationToken);
        if (workspace == null) return Result.Failure(WorkspaceError.NotFound);

        var members = request.Members;
        if (!members.Any()) return Result.Success();

        var lowerEmails = members.Select(m => m.Email.ToLower()).ToList();

        var users = await db.Users
            .Where(u => lowerEmails.Contains(u.Email.ToLower()) && u.DeletedAt == null)
            .ToListAsync(cancellationToken);

        if (users.Count != lowerEmails.Count)
        {
            return Result.Failure(Error.NotFound("User.NotFound", "One or more email addresses do not belong to a registered user. Check for typos."));
        }

        var existingUserIds = await db.WorkspaceMembers
            .Where(wm => wm.ProjectWorkspaceId == workspace.Id && wm.DeletedAt == null)
            .Select(wm => wm.UserId)
            .ToHashSetAsync(cancellationToken);

        var newMembersToInsert = new List<WorkspaceMember>();
        var memberRoleByEmail = members.ToDictionary(m => m.Email, m => m.Role, StringComparer.OrdinalIgnoreCase);
        foreach (var user in users)
        {
            if (existingUserIds.Contains(user.Id)) continue;
            var requestedRole = memberRoleByEmail[user.Email];
            var newMember = new WorkspaceMember(
                userId: user.Id,
                projectWorkspaceId: workspace.Id,
                role: requestedRole,
                status: MembershipStatus.Active,
                creatorId: context.CurrentMember.Id,
                joinMethod: "Invite"
            );

            newMembersToInsert.Add(newMember);
        }

        if (newMembersToInsert.Count == 0)
        {
            return Result.Failure(Error.Conflict("Member.AlreadyExists", "All specified users are already members of this workspace."));
        }

        if (newMembersToInsert.Count > 0)
        {
            await db.WorkspaceMembers.AddRangeAsync(newMembersToInsert, cancellationToken);
            var affected = await db.SaveChangesAsync(cancellationToken);

            if (affected > 0)
            {
                await cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceMembersTag(request.WorkspaceId), cancellationToken);

                var userLookup = users.ToDictionary(u => u.Id);
                var records = newMembersToInsert
                    .Select(wm => MemberRecord.FromDomain(wm, userLookup[wm.UserId]))
                    .ToList();
                await realtimeService.NotifyEntitiesUpdatedAsync(
                    request.WorkspaceId,
                    new EntityBatchUpdate { Members = records },
                    cancellationToken
                );
            }
        }

        return Result.Success();
    }
}



