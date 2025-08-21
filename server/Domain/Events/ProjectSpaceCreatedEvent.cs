using Domain.Common;
using System;

namespace Domain.Events;

public class ProjectSpaceCreatedEvent : DomainEventBase
{
    public Guid SpaceId { get; }
    public Guid WorkspaceId { get; }
    public Guid CreatorId { get; }

    public ProjectSpaceCreatedEvent(Guid spaceId, Guid workspaceId, Guid creatorId)
    {
        SpaceId = spaceId;
        WorkspaceId = workspaceId;
        CreatorId = creatorId;
    }
}