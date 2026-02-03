using Domain.Common;
using Domain.Enums.RelationShip;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities.Relationship;

public class EntityAccess : Entity
{
    public Guid WorkspaceMemberId { get; private set; }
    public Guid EntityId { get; private set; }
    public EntityLayerType EntityLayer { get; private set; }
    public AccessLevel AccessLevel { get; private set; }

    private EntityAccess() { } // EF
    private EntityAccess(Guid workspaceMemberId, Guid entityId, EntityLayerType entityLayer, AccessLevel accessLevel,  Guid creatorId)
    {
        WorkspaceMemberId = workspaceMemberId;
        EntityId = entityId;
        EntityLayer = entityLayer;
        AccessLevel = accessLevel;
        CreatorId = creatorId;
    }
    public static EntityAccess Create(Guid workspaceMemberId, Guid entityId, EntityLayerType entityLayer, AccessLevel accessLevel, Guid creatorId)
    {
        return new EntityAccess(workspaceMemberId, entityId, entityLayer, accessLevel, creatorId);
    }
    public void UpdateAccessLevel(AccessLevel newAccessLevel)
    {
        AccessLevel = newAccessLevel;
        UpdateTimestamp();
    }
    public void Remove()
    {
        var now = DateTimeOffset.UtcNow;
        DeletedAt = now;
        UpdateTimestamp();
    }

}
