using Domain.Enums;
using Domain.Enums.RelationShip;
using Domain.Enums.Workspace;

namespace Application.Common;

public record class PermissionContext
{
    public required Guid UserId { get; init; }
    public required Guid WorkspaceId { get; init; }
    public required Guid? EntityId { get; init; }
    public required EntityType EntityType { get; init; }

    // Workspace
    public required Role WorkspaceRole { get; init; }
    public required bool IsWorkspaceOwner { get; init; }
    public required bool IsWorkspaceAdmin { get; init; }
    public required bool IsUserSuspendedInWorkspace { get; init; }

    // Entity access
    public required AccessLevel? EntityAccess { get; init; }
    public required bool IsEntityManager { get; init; }
    public required bool IsEntityEditor { get; init; }
    public required bool IsEntityViewer { get; init; }

    // Chat room
    public required ChatRoomRole? ChatRoomRole { get; init; }
    public required bool IsChatRoomOwner { get; init; }
    public required bool IsUserBannedFromChatRoom { get; init; }
    public required bool IsUserMutedInChatRoom { get; init; }

    // Entity state
    public required bool IsCreator { get; init; }
    public required bool IsEntityArchived { get; init; }
    public required bool IsEntityPrivate { get; init; }

}
