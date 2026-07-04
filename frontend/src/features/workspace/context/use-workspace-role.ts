import { useSyncExternalStore } from "react";
import { autorun } from "mobx";
import { useWorkspace } from "./workspace-context";
import { useUser } from "@/features/auth/auth-api";
import { useStore } from "@/stores/root.store";
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
  const rootStore = useStore();

  // Read role from the MobX memberStore (Bootstrap + real-time Delta) — fall back to workspace
  // cache. useSyncExternalStore + autorun subscribes directly, independent of whether the calling
  // component happens to be wrapped in mobx-react-lite's observer() — this hook is consumed by
  // several plain (non-observer) components, so it can't rely on an ambient MobX render tracker.
  const myMember = useSyncExternalStore(
    (onStoreChange) => autorun(() => { rootStore.memberStore.all; onStoreChange(); }),
    () => (currentUser?.id ? rootStore.memberStore.getByUserId(currentUser.id) : undefined),
  );
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
    canManageMembers:  isAdmin,
    canInviteMembers:  isAdmin,
    canCreateSpace:    isAdmin,
    canDeleteSpace:    isAdmin,
    canCreateContent:  isMember,
  };
}
