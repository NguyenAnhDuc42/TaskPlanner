using Domain.Enums;
using Domain.Enums.RelationShip;
using Domain.Enums.Workspace;

namespace Application.Common;

public record class PermissionContext
{
    // User info
    public required Guid UserId { get; init; }
    public required Guid WorkspaceId { get; init; }

    // Current entity
    public required Guid? EntityId { get; init; }
    public required EntityType EntityType { get; init; }

    // Workspace-level
    public required Role WorkspaceRole { get; init; }
    public required bool IsWorkspaceOwner { get; init; }
    public required bool IsWorkspaceAdmin { get; init; }
    public required bool IsUserSuspendedInWorkspace { get; init; }

    // Entity-level access
    public required AccessLevel? EntityAccess { get; init; }
    public required bool IsEntityManager { get; init; }
    public required bool IsEntityEditor { get; init; }
    public required bool IsEntityViewer { get; init; }

    // Chat room specific
    public required ChatRoomRole? ChatRoomRole { get; init; }
    public required bool IsChatRoomOwner { get; init; }
    public required bool IsUserBannedFromChatRoom { get; init; }
    public required bool IsUserMutedInChatRoom { get; init; }

    // Entity state
    public required bool IsCreator { get; init; }
    public required bool IsEntityArchived { get; init; }
    public required bool IsEntityPrivate { get; init; }
    public required DateTimeOffset? EntityCreatedAt { get; init; }

    // Additional entity context (for cross-entity checks)
    public required int? ActiveChildCount { get; init; }  // For archive rules (list has active tasks?)
    public required bool HasParentEntity { get; init; }    // Parent exists?
    public required Guid? ParentEntityId { get; init; }    // Parent ID for move validation

    // Message-specific
    public required int MessageAgeMinutes { get; init; }   // How old is this message?
    public required bool IsMessagePinned { get; init; }
}
