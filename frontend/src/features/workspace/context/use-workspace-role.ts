import { createContext, useCallback, useContext, useMemo, useSyncExternalStore } from "react";
import { autorun } from "mobx";
import { useWorkspace } from "./workspace-context";
import { useUser } from "@/features/auth/auth-api";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import type { WorkspaceRootStore } from "@/stores/workspace-root.store";
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
  canCreateContent: boolean;
  canDeleteSpace: boolean;
}

const NO_ROLE: WorkspaceRole = {
  role: undefined,
  isOwner: false,
  isAdmin: false,
  isMember: false,
  isGuest: false,
  canEditWorkspace: false,
  canManageMembers: false,
  canInviteMembers: false,
  canCreateSpace: false,
  canDeleteSpace: false,
  canCreateContent: false,
};

export const WorkspaceRoleContext = createContext<WorkspaceRole>(NO_ROLE);

function subscribeToMembers(rootStore: WorkspaceRootStore, onStoreChange: () => void) {
  return autorun(() => { void rootStore.memberStore.all; onStoreChange(); });
}

export function useComputeWorkspaceRole(): WorkspaceRole {
  const { workspace } = useWorkspace();
  const { data: currentUser } = useUser();
  const rootStore = useWorkspaceRootStore();

  const subscribe = useCallback(
    (onStoreChange: () => void) => subscribeToMembers(rootStore, onStoreChange),
    [rootStore],
  );
  const getSnapshot = useCallback(
    () => (currentUser?.id ? rootStore.memberStore.getByUserId(currentUser.id) : undefined),
    [rootStore, currentUser],
  );

  const myMember = useSyncExternalStore(subscribe, getSnapshot);
  const role = (myMember?.role as Role | undefined) ?? workspace?.role;

  return useMemo(() => {
    const isOwner  = role === "Owner";
    const isAdmin  = isOwner || role === "Admin";
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
  }, [role]);
}

export function useWorkspaceRole(): WorkspaceRole {
  return useContext(WorkspaceRoleContext);
}
