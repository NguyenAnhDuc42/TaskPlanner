using Domain.Common;
using Domain.Enums.RelationShip;

namespace Domain.Entities;

public class EntityAccess : Entity
{
    public Guid ProjectWorkspaceId { get; private set; }
    public Guid WorkspaceMemberId { get; private set; }
    
    public Guid? ProjectSpaceId { get; private set; }
    public Guid? ProjectFolderId { get; private set; }
    public Guid? ProjectTaskId { get; private set; }
    public AccessLevel AccessLevel { get; private set; }

    private EntityAccess() { } // EF

    private EntityAccess(Guid projectWorkspaceId, Guid workspaceMemberId, Guid? projectSpaceId, Guid? projectFolderId, Guid? projectTaskId, AccessLevel accessLevel, Guid creatorId)
        : base(Guid.NewGuid())
    {
        ProjectWorkspaceId = projectWorkspaceId;
        WorkspaceMemberId = workspaceMemberId;
        ProjectSpaceId = projectSpaceId;
        ProjectFolderId = projectFolderId;
        ProjectTaskId = projectTaskId;
        AccessLevel = accessLevel;
        InitializeAudit(creatorId);
    }

    public void Update(AccessLevel accessLevel)
    {
        AccessLevel = accessLevel;
        UpdateTimestamp();
    }

    public static EntityAccess Create(Guid projectWorkspaceId, Guid workspaceMemberId, Guid? projectSpaceId, Guid? projectFolderId, Guid? projectTaskId, AccessLevel accessLevel, Guid creatorId)
    {
        return new EntityAccess(projectWorkspaceId, workspaceMemberId, projectSpaceId, projectFolderId, projectTaskId, accessLevel, creatorId);
    }

    public void UpdateAccessLevel(AccessLevel newAccessLevel)
    {
        AccessLevel = newAccessLevel;
        UpdateTimestamp();
    }

    public void Remove()
    {
        SoftDelete();
    }
}
