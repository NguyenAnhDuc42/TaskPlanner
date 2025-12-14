using System;

namespace Application.Common;

[Flags]
public enum PermissionDataNeeds
{
    None = 0,
    WorkspaceRole = 1 << 0,          // Fetch workspace membership role
    EntityAccess = 1 << 1,           // Fetch entity member access level
    IsCreator = 1 << 2,              // Check if user is entity creator
    EntityState = 1 << 3,            // Get IsArchived, IsPrivate from entity
    ChatRoomRole = 1 << 4,           // Fetch chat room specific role
    ChatRoomMemberStatus = 1 << 5    // Get IsBanned, IsMuted status
}
