using Microsoft.EntityFrameworkCore;
using Domain;

namespace Application;

public class PermissionService(TaskPlanDbContext db, WorkspaceContext context)
{

    public async Task<bool> VerifyAsync(
        Role requiredRole,
        Guid? spaceId = null,
        AccessLevel? requiredAccess = null,
        CancellationToken ct = default)
    {
        var currentMember = context.CurrentMember;
        if (currentMember == null) return false;

        // 1. Check workspace level role
        if (currentMember.Role.IsAtLeast(requiredRole)) return true;

        // 2. Check space level access
        if (spaceId.HasValue && requiredAccess.HasValue)
        {
            var space = await db.ProjectSpaces.AsNoTracking().FirstOrDefaultAsync(s => s.Id == spaceId.Value, ct);
            if (space == null) return false;

            if (!space.IsPrivate) return true;

            var access = await db.EntityAccesses.AsNoTracking()
                .FirstOrDefaultAsync(ea => ea.ProjectSpaceId == spaceId.Value && ea.WorkspaceMemberId == currentMember.Id && ea.DeletedAt == null, ct);

            if (access == null) return false;

            return access.AccessLevel.IsAtLeast(requiredAccess.Value);
        }

        return false;
    }
}
