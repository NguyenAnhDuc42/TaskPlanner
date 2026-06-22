using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace Application;

public class PermissionService(TaskPlanDbContext db, WorkspaceContext context, ILogger<PermissionService> logger)
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
        if (logger.IsEnabled(LogLevel.Debug)) 
        logger.LogDebug("Verifying permissions for user {UserId} with role {UserRole} against required role {RequiredRole} and access level {RequiredAccess} in space {SpaceId}", currentMember.UserId, currentMember.Role, requiredRole, requiredAccess, spaceId);

        if (!currentMember.Role.IsAtLeast(requiredRole)) return false;

        if (currentMember.Role.IsAtLeast(Role.Admin)) return true;

        if (!spaceId.HasValue) return true; 

        var info = await db.ProjectSpaces.AsNoTracking()
            .Where(s => s.Id == spaceId.Value && s.DeletedAt == null)
            .Select(s => new {
                s.IsPrivate,
                AccessLevel = db.EntityAccesses
                    .Where(ea => ea.ProjectSpaceId == s.Id
                              && ea.WorkspaceMemberId == currentMember.Id
                              && ea.DeletedAt == null)
                    .Select(ea => (AccessLevel?)ea.AccessLevel)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (info is null) return false;
        if (!info.IsPrivate) return true;
        if (info.AccessLevel is null) return false;
        if (creatorId.HasValue && currentMember.Id == creatorId.Value) return true;
        return info.AccessLevel.Value.IsAtLeast(requiredAccess!.Value);
    }
}
