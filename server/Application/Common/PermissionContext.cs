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

    public VisibilityLevel? CurrentLevel => VisibilityChain.FirstOrDefault();

    public AccessLevel? GetEffectiveAccessAtCurrentLevel()
    {
        if (CurrentLevel?.IsBlocking == true) return null;
        return CurrentLevel?.Access;
    }

    // Workspace
    public Role WorkspaceRole { get; set; }
    public bool IsWorkspaceOwner  => WorkspaceRole == Role.Owner;
    public bool IsWorkspaceAdmin  => WorkspaceRole == Role.Owner || WorkspaceRole == Role.Admin;
    public bool IsUserSuspendedInWorkspace { get; set; }

    // Relationship
    // Relationship
    public AccessLevel? EntityAccess { get; set; } // The most specific override found
    public bool IsPrivacyBlocked { get; set; }     // True if hit private layer without override
    
    // Detailed Visibility Chain (inspired by feedback)
    public List<VisibilityLevel> VisibilityChain { get; set; } = new();
    public VisibilityLevel? BlockedAt => VisibilityChain.FirstOrDefault(v => v.IsBlocking);

    public bool IsEntityManager => EntityAccess == AccessLevel.Manager && !IsPrivacyBlocked;
    public bool IsEntityEditor => EntityAccess == AccessLevel.Editor && !IsPrivacyBlocked;
    public bool IsEntityViewer => EntityAccess == AccessLevel.Viewer && !IsPrivacyBlocked;

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

public record VisibilityLevel
{
    public Guid Id { get; init; }
    public EntityType Type { get; init; }
    public bool IsPrivate { get; init; }
    public bool HasExplicitMembership { get; init; }
    public AccessLevel? Access { get; init; }
    public bool IsBlocking => IsPrivate && !HasExplicitMembership;
}
