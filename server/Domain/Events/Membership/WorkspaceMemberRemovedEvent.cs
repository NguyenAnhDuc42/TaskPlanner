using System;
using Domain.Enums.Workspace;

namespace Domain.Events.Membership;

public record WorkspaceMemberRemovedEvent : BaseDomainEvent
{
    public Guid UserId { get; init; }
    public Guid WorkspaceId { get; init; }
    
    public WorkspaceMemberRemovedEvent(Guid userId, Guid workspaceId) : base(workspaceId)
    {
        UserId = userId;
        WorkspaceId = workspaceId;
    }
}
