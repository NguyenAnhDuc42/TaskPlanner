using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application;

public class PermissionService(TaskPlanDbContext db, WorkspaceContext context, ILogger<PermissionService> logger)
{
    // Used by Create* handlers that haven't pre-loaded the space entity.
    // Queries ProjectSpaces + EntityAccesses in one round-trip.
    public async Task<bool> VerifyAsync(
        Role requiredRole,
        Guid? spaceId = null,
        AccessLevel? requiredAccess = null,
        Guid? creatorId = null,
        CancellationToken cancellationToken = default)
    {
        if (spaceId.HasValue != requiredAccess.HasValue)
            throw new ArgumentException("spaceId and requiredAccess must both be provided or both be null");

        var currentMember = context.CurrentMember;
        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("VerifyAsync: user {UserId} role {Role} required {RequiredRole} space {SpaceId}", currentMember.UserId, currentMember.Role, requiredRole, spaceId);

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
        return CheckSpaceAccess(info.IsPrivate, info.AccessLevel, requiredAccess, creatorId);
    }

    // Used by Update*/Delete* handlers that already loaded the entity (and thus have IsPrivate + callerAccessLevel).
    // No DB query — pure logic.
    public bool Verify(
        Role requiredRole,
        bool isPrivate,
        AccessLevel? callerAccessLevel,
        AccessLevel? requiredAccess = null,
        Guid? creatorId = null)
    {
        var currentMember = context.CurrentMember;
        if (!currentMember.Role.IsAtLeast(requiredRole)) return false;
        if (currentMember.Role.IsAtLeast(Role.Admin)) return true;
        return CheckSpaceAccess(isPrivate, callerAccessLevel, requiredAccess, creatorId);
    }

    private bool CheckSpaceAccess(bool isPrivate, AccessLevel? callerAccessLevel, AccessLevel? requiredAccess, Guid? creatorId)
    {
        var currentMember = context.CurrentMember;
        if (!isPrivate) return true;
        if (callerAccessLevel is null) return false;
        if (creatorId.HasValue && currentMember.Id == creatorId.Value) return true;
        return requiredAccess.HasValue && callerAccessLevel.Value.IsAtLeast(requiredAccess.Value);
    }
}
