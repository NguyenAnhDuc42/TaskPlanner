using Microsoft.EntityFrameworkCore;

namespace Application;

public class AddMembersHandler(TaskPlanDbContext db, WorkspaceContext context,PermissionService permissionService,RealtimeService realtimeService) : ICommandHandler<AddMembersCommand>
{
    public async Task<Result> Handle(AddMembersCommand request, CancellationToken cancellationToken)
    {
        var hasAccess = await permissionService.VerifyAsync(Role.Admin,cancellationToken: cancellationToken);
        if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);

        var workspace = await db.ProjectWorkspaces.FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && w.DeletedAt == null,cancellationToken);
        if (workspace == null) return Result.Failure(WorkspaceError.NotFound);

        var members = request.Members;
        if (!members.Any()) return Result.Success();

        var emails = members.Select(m => m.Email).ToList();

        var users = await db.Users
            .Where(u => emails.Contains(u.Email) && u.DeletedAt == null)
            .ToListAsync(cancellationToken);

        if (!users.Any()) return Result.Success();

        var existingUserIds = await db.WorkspaceMembers
            .Where(wm => wm.ProjectWorkspaceId == workspace.Id && wm.DeletedAt == null)
            .Select(wm => wm.UserId)
            .ToHashSetAsync(cancellationToken);

        var newMembersToInsert = new List<WorkspaceMember>();
        var memberRoleByEmail = members.ToDictionary(m => m.Email, m => m.Role);
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

        if (newMembersToInsert.Count > 0)
        {
            await db.WorkspaceMembers.AddRangeAsync(newMembersToInsert, cancellationToken);
            var affected = await db.SaveChangesAsync(cancellationToken);

            if (affected > 0)
            {
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



