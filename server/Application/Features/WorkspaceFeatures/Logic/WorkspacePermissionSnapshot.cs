using Domain.Enums;

namespace Application.Features.WorkspaceFeatures.Logic;

public record WorkspacePermissionSnapshot
{
    public Guid WorkspaceId { get; init; }
    public Role Role { get; init; } = Role.None;
    public bool IsOwned { get; init; }
    public bool IsMember { get; init; }
    public bool IsSuspended { get; init; }
    public bool CanViewHierarchy { get; init; }
    public bool CanManageWorkspace { get; init; }
    public bool CanManageMembers { get; init; }
    public bool CanCreateSpace { get; init; }
    public bool CanUpdateWorkspace { get; init; }
    public bool CanDeleteWorkspace { get; init; }
}

