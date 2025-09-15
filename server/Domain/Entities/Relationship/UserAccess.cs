using System;
using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Relationship;

public class UserAccess : Entity
{
    public Guid UserId { get; private set; }
    public Guid EntityId { get; private set; }
    public EntityType EntityType { get; private set; }
    public AccessLevel AccessLevel { get; private set; }
    public bool CanView { get; private set; } = true;

    private UserAccess() { } // EF
    private UserAccess(Guid userId, Guid entityId, EntityType entityType, AccessLevel accessLevel)
    {
        UserId = userId;
        EntityId = entityId;
        EntityType = entityType;
        AccessLevel = accessLevel;
    }

    public static UserAccess Create(Guid userId, Guid entityId, EntityType entityType, AccessLevel? accessLevel)
    {
        return new UserAccess(userId, entityId, entityType, accessLevel ?? AccessLevel.Viewer);
    }

    public void UpdateAccessLevel(AccessLevel newAccessLevel)
    {
        AccessLevel = newAccessLevel;
        UpdateTimestamp();
    }
    public void ChangeViewPermission(bool canView)
    {
        CanView = canView;
        UpdateTimestamp();
    }
}
