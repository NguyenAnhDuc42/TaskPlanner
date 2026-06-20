import { useWorkspace } from "./workspace-provider";
import type { Role } from "@/types/role";

export interface WorkspaceRole {
  role: Role | undefined;
  // Role tier booleans
  isOwner: boolean;
  isAdmin: boolean;   // Admin or Owner
  isMember: boolean;  // Member, Admin, or Owner
  isGuest: boolean;
  // Permission flags (from backend WorkspaceRecord)
  canEditWorkspace: boolean;    // Owner only — rename/recolor/delete workspace
  canManageMembers: boolean;    // Admin+
  canInviteMembers: boolean;    // Admin+
  canCreateSpace: boolean;      // Admin+
  canDeleteSpace: boolean;      // Admin+
  canCreateContent: boolean;    // Member+ — folders, tasks
}

export function useWorkspaceRole(): WorkspaceRole {
  const { workspace } = useWorkspace();

  const role = workspace?.role;

  const isOwner  = workspace?.isOwned ?? role === "Owner";
  const isAdmin  = isOwner || workspace?.canEdit === true || role === "Admin";
  const isMember = isAdmin || role === "Member";
  const isGuest  = !isMember && !!role; // has a role but below Member

  return {
    role,
    isOwner,
    isAdmin,
    isMember,
    isGuest,
    canEditWorkspace:  isOwner,
    canManageMembers:  workspace?.canManageMembers ?? isAdmin,
    canInviteMembers:  workspace?.canInvite ?? isAdmin,
    canCreateSpace:    isAdmin,
    canDeleteSpace:    isAdmin,
    canCreateContent:  isMember,
  };
}
