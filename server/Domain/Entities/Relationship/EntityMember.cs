using Domain.Common;
using Domain.Enums.RelationShip;

namespace Domain.Entities.Relationship;

public class EntityMember : Composite
{
    public Guid UserId { get; private set; }
    public Guid EntityId { get; private set; }
    public EntityLayerType EntityType { get; private set; }
    public AccessLevel AccessLevel { get; private set; } = AccessLevel.Viewer;
    public bool NotificationsEnabled { get; private set; } = true;
    public Guid CreatedBy { get; private set; }

    private EntityMember() { } // EF
    private EntityMember(Guid userId, Guid entityId, EntityLayerType entityType, AccessLevel accessLevel, Guid createdBy)
    {
        UserId = userId;
        EntityId = entityId;
        EntityType = entityType;
        AccessLevel = accessLevel;
        CreatedBy = createdBy;
    }

    public static EntityMember AddMember(Guid userId, Guid entityId, EntityLayerType entityType, AccessLevel accessLevel, Guid createdBy)
    {
        return new EntityMember(userId, entityId, entityType, accessLevel, createdBy);
    }

    public void UpdateAccessLevel(AccessLevel newAccessLevel)
    {
        AccessLevel = newAccessLevel;
        UpdateTimestamp();
    }
}

