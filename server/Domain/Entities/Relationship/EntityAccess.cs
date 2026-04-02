using Domain.Common;
using Domain.Enums.RelationShip;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities.Relationship;

public class EntityAccess : Entity
{
    public Guid ProjectWorkspaceId { get; private set; }
    public Guid WorkspaceMemberId { get; private set; }
    public Guid EntityId { get; private set; }
    public EntityLayerType EntityLayer { get; private set; }
    public AccessLevel AccessLevel { get; private set; }

    private EntityAccess() { } // EF
    private EntityAccess(Guid projectWorkspaceId, Guid workspaceMemberId, Guid entityId, EntityLayerType entityLayer, AccessLevel accessLevel, Guid creatorId)
    {
        ProjectWorkspaceId = projectWorkspaceId;
        WorkspaceMemberId = workspaceMemberId;
        EntityId = entityId;
        EntityLayer = entityLayer;
        AccessLevel = accessLevel;
        CreatorId = creatorId;
    }
    public void Update(AccessLevel accessLevel, Guid updaterId)
    {
        AccessLevel = accessLevel;
        UpdateTimestamp();
    }
    public static EntityAccess Create(Guid projectWorkspaceId, Guid workspaceMemberId, Guid entityId, EntityLayerType entityLayer, AccessLevel accessLevel, Guid creatorId)
    {
        return new EntityAccess(projectWorkspaceId, workspaceMemberId, entityId, entityLayer, accessLevel, creatorId);
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
