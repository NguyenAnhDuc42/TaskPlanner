using System;
using Domain.Enums;
using Domain.Enums.Workspace;

namespace Domain.Events.Membership;

public record WorkspaceMemberRoleChangedEvent : BaseDomainEvent
{
    public Guid UserId { get; init; }
    public Guid WorkspaceId { get; init; }
    public Role OldRole { get; init; }
    public Role NewRole { get; init; }
    
    public WorkspaceMemberRoleChangedEvent(Guid userId, Guid workspaceId, Role oldRole, Role newRole) : base(workspaceId)
    {
        UserId = userId;
        WorkspaceId = workspaceId;
        OldRole = oldRole;
        NewRole = newRole;
    }
}
