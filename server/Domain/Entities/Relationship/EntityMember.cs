using Domain.Common;
using Domain.Enums.RelationShip;

namespace Domain.Entities.Relationship;

public class EntityMember : Composite
{
    public Guid UserId { get; private set; }
    public Guid LayerId { get; private set; }
    public EntityLayerType LayerType { get; private set; }
    public AccessLevel AccessLevel { get; private set; } = AccessLevel.Viewer;
    public bool NotificationsEnabled { get; private set; } = true;
    public Guid CreatorId { get; private set; }

    private EntityMember() { } // EF
    private EntityMember(Guid userId, Guid layerId, EntityLayerType layerType, AccessLevel accessLevel, Guid creatorId)
    {
        UserId = userId;
        LayerId = layerId;
        LayerType = layerType;
        AccessLevel = accessLevel;
        CreatorId = creatorId;
    }

    public static EntityMember AddMember(Guid userId, Guid layerId, EntityLayerType layerType, AccessLevel accessLevel, Guid creatorId)
    {
        return new EntityMember(userId, layerId, layerType, accessLevel, creatorId);
    }

    public void UpdateAccessLevel(AccessLevel newAccessLevel)
    {
        AccessLevel = newAccessLevel;
        UpdateTimestamp();
    }
}

