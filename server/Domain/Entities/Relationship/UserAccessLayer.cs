using System;
using System.ComponentModel.DataAnnotations;
using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Relationship;

public class UserAccessLayer : Entity
{
    [Required] public Guid UserId { get; private set; }
    [Required] public Guid EntityId { get; private set; }
    [Required] public EntityType EntityType { get; private set; }
    [Required] public AccessLevel AccessLevel { get; private set; } 

    private UserAccessLayer() { } // EF
    private UserAccessLayer(Guid userId, Guid entityId, EntityType entityType, AccessLevel accessLevel)
    {
        UserId = userId;
        EntityId = entityId;
        EntityType = entityType;
        AccessLevel = accessLevel;
    }

    public static UserAccessLayer Create(Guid userId, Guid entityId, EntityType entityType, AccessLevel? accessLevel)
    {
        return new UserAccessLayer(userId, entityId, entityType, accessLevel ?? AccessLevel.Viewer);
    }

    public void UpdateAccessLevel(AccessLevel newAccessLevel)
    {
        AccessLevel = newAccessLevel;
        UpdateTimestamp();
    }
}
