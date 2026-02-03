using Domain.Enums;
using Domain.Enums.RelationShip;

namespace Domain.Permission;
public class PermissionContext
{
    public Guid UserId { get; init; }
    public Guid WorkspaceId { get; init; }
    public Guid WorkspaceMemberId { get; init; }
    public Role Role { get; init; }

    private readonly Dictionary<(EntityLayerType, Guid), AccessLevel> _explicitAccess;

    public PermissionContext(
        Guid userId,
        Guid workspaceId,
        Guid workspaceMemberId,
        Role role,
        IEnumerable<(EntityLayerType type, Guid entityId, AccessLevel level)> accesses)
    {
        UserId = userId;
        WorkspaceId = workspaceId;
        WorkspaceMemberId = workspaceMemberId;
        Role = role;
        _explicitAccess = accesses.ToDictionary(x => (x.type, x.entityId), x => x.level);
    }

    public AccessLevel? GetExplicitAccess(EntityLayerType type, Guid entityId)
    {
        _explicitAccess.TryGetValue((type, entityId), out var level);
        return level == AccessLevel.None ? null : level;
    }
}