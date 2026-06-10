using Microsoft.EntityFrameworkCore;
using Domain;

namespace Application;

public class PermissionService(TaskPlanDbContext db, WorkspaceContext context)
{

    public async Task<bool> VerifyAsync(
        Role requiredRole,
        Guid? spaceId = null,
        AccessLevel? requiredAccess = null,
        Guid? creatorId = null,
        CancellationToken cancellationToken = default)
    {
        if (spaceId.HasValue != requiredAccess.HasValue) throw new ArgumentException("spaceId and requiredAccess must both be provided or both be null");

        var currentMember = context.CurrentMember;
        if (currentMember is null) return false;

        if (currentMember.Role.IsAtLeast(requiredRole)) return true;

        if (!spaceId.HasValue) return false;

        var spaceInfo = await db.ProjectSpaces.AsNoTracking()
            .Where(s => s.Id == spaceId.Value && s.DeletedAt == null)
            .Select(s => new { s.IsPrivate })
            .FirstOrDefaultAsync(cancellationToken);

        if (spaceInfo is null) return false;
        if (!spaceInfo.IsPrivate) return true;

        var access = await db.EntityAccesses.AsNoTracking()
            .Where(ea => ea.ProjectSpaceId == spaceId.Value
                    && ea.WorkspaceMemberId == currentMember.Id
                    && ea.DeletedAt == null)
            .Select(ea => new { ea.AccessLevel })
            .FirstOrDefaultAsync(cancellationToken);

        if (access is null) return false;
        if (creatorId.HasValue && currentMember.Id == creatorId.Value) return true;
        return access.AccessLevel.IsAtLeast(requiredAccess!.Value);
    }
}
