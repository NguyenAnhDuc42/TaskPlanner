using System;
using Domain.Enums;
using Domain.Enums.RelationShip;

namespace Domain.Events.Membership;

public record EntityAccessChangedEvent : BaseDomainEvent
{
    public Guid UserId { get; init; }
    public Guid EntityId { get; init; }
    public EntityType EntityType { get; init; }
    public AccessLevel OldAccess { get; init; }
    public AccessLevel NewAccess { get; init; }
    
    public EntityAccessChangedEvent(Guid userId, Guid entityId, EntityType entityType, AccessLevel oldAccess, AccessLevel newAccess) : base(entityId)
    {
        UserId = userId;
        EntityId = entityId;
        EntityType = entityType;
        OldAccess = oldAccess;
        NewAccess = newAccess;
    }
}
