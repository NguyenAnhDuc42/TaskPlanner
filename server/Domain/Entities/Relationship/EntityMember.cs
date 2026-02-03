using Domain.Common;
using Domain.Enums.RelationShip;
using System;

namespace Domain.Entities.Relationship;

/// <summary>
/// [LEGACY] EntityMember is deprecated. Use EntityAccess instead.
/// This class will be removed in a future version.
/// EntityAccess provides a cleaner separation between workspace membership and entity-level access.
/// </summary>
[Obsolete("EntityMember is legacy. Use EntityAccess instead. Will be removed in v2.0.", false)]
public class EntityMember : Composite
{
    public Guid UserId { get; private set; }
    public Guid LayerId { get; private set; }
    public EntityLayerType LayerType { get; private set; }
    public AccessLevel AccessLevel { get; private set; } = AccessLevel.Viewer;
    public bool NotificationsEnabled { get; private set; } = true;

    private EntityMember() { } // EF
    private EntityMember(Guid userId, Guid layerId, EntityLayerType layerType, AccessLevel accessLevel, Guid creatorId)
    {
        UserId = userId;
        LayerId = layerId;
        LayerType = layerType;
        AccessLevel = accessLevel;
        CreatorId = creatorId;
    }

    [Obsolete("Use EntityAccess instead.")]
    public static EntityMember AddMember(Guid userId, Guid layerId, EntityLayerType layerType, AccessLevel accessLevel, Guid creatorId)
    {
        return new EntityMember(userId, layerId, layerType, accessLevel, creatorId);
    }

    [Obsolete("Use EntityAccess instead.")]
    public void UpdateAccessLevel(AccessLevel newAccessLevel)
    {
        var oldAccess = AccessLevel;
        AccessLevel = newAccessLevel;
        UpdateTimestamp();
        
        if (oldAccess != newAccessLevel)
        {
            AddDomainEvent(new Events.Membership.EntityMemberAccessChangedEvent(
                UserId, LayerId, (Domain.Enums.EntityType)Enum.Parse(typeof(Domain.Enums.EntityType), LayerType.ToString()), oldAccess, newAccessLevel));
        }
    }

    [Obsolete("Use EntityAccess instead.")]
    public void UpdateNotificationsEnabled(bool enabled)
    {
        NotificationsEnabled = enabled;
        UpdateTimestamp();
    }
}

