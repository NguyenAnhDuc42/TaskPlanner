using Domain.Enums;
using Domain.Enums.RelationShip;
using Domain.Enums.Workspace;

namespace Application.Common;

public record class PermissionContext
{
    public Guid UserId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid? EntityId { get; set; }
    public EntityType? EntityType { get; set; }

    // Workspace
    public Role WorkspaceRole { get; set; }
    public bool IsWorkspaceOwner  => WorkspaceRole == Role.Owner;
    public bool IsWorkspaceAdmin  => WorkspaceRole == Role.Owner;
    public bool IsUserSuspendedInWorkspace { get; set; }

    // Entity access
    public AccessLevel? EntityAccess { get; set; }
    public bool IsEntityManager => EntityAccess == AccessLevel.Manager;
    public bool IsEntityEditor => EntityAccess == AccessLevel.Editor;
    public bool IsEntityViewer => EntityAccess == AccessLevel.Viewer;

    // Chat room
    public ChatRoomRole? ChatRoomRole { get; set; }
    public bool IsChatRoomOwner { get; set; }
    public bool IsUserBannedFromChatRoom { get; set; }
    public bool IsUserMutedInChatRoom { get; set; }

    // Entity state
    public bool IsCreator { get; set; }
    public bool IsEntityArchived { get; set; }
    public bool IsEntityPrivate { get; set; }

}
