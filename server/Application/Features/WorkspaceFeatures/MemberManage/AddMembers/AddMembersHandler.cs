using Microsoft.EntityFrameworkCore;

namespace Application;

public class AddMembersHandler(TaskPlanDbContext db, WorkspaceContext context) : ICommandHandler<AddMembersCommand>
{
    public async Task<Result> Handle(AddMembersCommand request, CancellationToken ct)
    {
        if (context.CurrentMember.Role > Role.Admin)
            return Result.Failure(MemberError.DontHavePermission);

        var workspace = await db.ProjectWorkspaces
            .ById(request.workspaceId)
            .FirstOrDefaultAsync(ct);

        if (workspace == null) return Result.Failure(WorkspaceError.NotFound);

        var members = request.members;
        if (!members.Any()) return Result.Success();

        var emails = members.Select(m => m.email).ToList();

        // 1. Find all requested users by email
        var users = await db.Users
            .Where(u => emails.Contains(u.Email) && u.DeletedAt == null)
            .ToListAsync(ct);

        if (!users.Any()) return Result.Success();

        // 2. Filter out users who are already in the workspace
        var existingUserIds = await db.WorkspaceMembers
            .Where(wm => wm.ProjectWorkspaceId == workspace.Id && wm.DeletedAt == null)
            .Select(wm => wm.UserId)
            .ToListAsync(ct);

        var newMembersToInsert = new List<WorkspaceMember>();

        foreach (var user in users)
        {
            if (existingUserIds.Contains(user.Id)) continue;

            // Find the role requested for this email
            var requestedRole = members.First(m => m.email == user.Email).role;

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

        if (newMembersToInsert.Any())
        {
            await db.WorkspaceMembers.AddRangeAsync(newMembersToInsert, ct);
            await db.SaveChangesAsync(ct);
        }

        return Result.Success();
    }
}



