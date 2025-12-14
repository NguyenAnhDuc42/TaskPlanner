using System;

namespace Domain.Events.Workspace;

public record WorkspaceDeletedEvent : BaseDomainEvent
{
    public Guid WorkspaceId { get; init; }
    
    public WorkspaceDeletedEvent(Guid workspaceId) : base(workspaceId)
    {
        WorkspaceId = workspaceId;
    }
}
