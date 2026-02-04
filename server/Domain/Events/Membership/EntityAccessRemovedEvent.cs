using System;
using Domain.Enums;
using Domain.Enums.RelationShip;

namespace Domain.Events.Membership;

public record EntityAccessRemovedEvent : BaseDomainEvent
{
    public Guid UserId { get; init; }
    public Guid EntityId { get; init; }
    public EntityType EntityType { get; init; }
    
    public EntityAccessRemovedEvent(Guid userId, Guid entityId, EntityType entityType) : base(entityId)
    {
        UserId = userId;
        EntityId = entityId;
        EntityType = entityType;
    }
}
