import type { Role } from "../role";
import type { Theme } from "../theme";

export interface WorkspaceRecord {
    id: string;
    name: string;
    icon?: string;
    color?: string;
    description?: string;
    role?: Role;
    theme?: Theme;
    isPinned?: boolean;
    isOwned?: boolean;
    canEdit?: boolean;
    canInvite?: boolean;
    canManageMembers?: boolean;
    canPinWorkspace?: boolean;
    memberCount?: number;
    isArchived?: boolean;
    isDashboardEnabled?: boolean;
    joinCode?: string;
    strictJoin?: boolean;
    membershipStatus?: "Active" | "Pending" | "Suspended" | "Invited";
}
