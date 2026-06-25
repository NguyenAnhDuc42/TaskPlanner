import { useSelector } from "react-redux";
import { useWorkspace } from "./workspace-context";
import { useUser } from "@/features/auth/auth-api";
import { memberSelectors } from "@/store/entityStore";
import type { Role } from "@/types/role";

export interface WorkspaceRole {
  role: Role | undefined;
  isOwner: boolean;
  isAdmin: boolean;
  isMember: boolean;
  isGuest: boolean;
  canEditWorkspace: boolean;
  canManageMembers: boolean;
  canInviteMembers: boolean;
  canCreateSpace: boolean;
  canDeleteSpace: boolean;
  canCreateContent: boolean;
}

export function useWorkspaceRole(): WorkspaceRole {
  const { workspace } = useWorkspace();
  const { data: currentUser } = useUser();

  // Read role from memberSlice (gets real-time SignalR updates) — fall back to workspace cache
  const allMembers = useSelector(memberSelectors.selectAll);
  const myMember = allMembers.find(m => m.userId === currentUser?.id);
  const role = (myMember?.role as Role | undefined) ?? workspace?.role;

  const isOwner  = workspace?.isOwned ?? role === "Owner";
  const isAdmin  = isOwner || workspace?.canEdit === true || role === "Admin";
  const isMember = isAdmin || role === "Member";
  const isGuest  = !isMember && !!role;

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
