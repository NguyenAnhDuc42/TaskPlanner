import type { Role } from "../role";

export interface WorkspaceRecord {
    id: string;
    name: string;
    icon?: string;
    color?: string;
    description?: string;
    // role/isPinned are the only two fields here that can't be derived client-side — irreducible
    // per-membership state, needed for the workspace list/switcher before a workspace is even
    // opened, where there's no MemberRecord available yet. Permission flags (canEdit/canInvite/
    // etc.) used to live here too but were pure derivations of role — use useWorkspaceRole()
    // instead, which computes them from the single source of truth (MemberRecord when inside an
    // open workspace, this role field as a fallback for the list/switcher).
    role?: Role;
    isPinned?: boolean;
    isArchived?: boolean;
    joinCode?: string;
    strictJoin?: boolean;
    membershipStatus?: "Active" | "Pending" | "Suspended" | "Invited";
    accessRevoked?: boolean;
}
