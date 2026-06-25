import { useMemo } from "react";
import { useSelector } from "react-redux";
import { entityAccessSelectors, memberSelectors } from "@/store/entityStore";
import { useWorkspaceRole } from "./use-workspace-role";
import { useUser } from "@/features/auth/auth-api";

export type SpaceAccessLevel = "None" | "Viewer" | "Editor" | "Manager";

const RANK: Record<SpaceAccessLevel, number> = { None: 0, Viewer: 1, Editor: 2, Manager: 3 };

export function useSpaceAccess(spaceId: string) {
  const { isAdmin } = useWorkspaceRole();
  const { data: currentUser } = useUser();
  const allMembers = useSelector(memberSelectors.selectAll);
  const allAccess  = useSelector(entityAccessSelectors.selectAll);

  const level = useMemo<SpaceAccessLevel>(() => {
    if (isAdmin) return "Manager";
    const myMember = allMembers.find(m => m.userId === currentUser?.id);
    if (!myMember) return "None";
    const ea = allAccess.find(a => a.spaceId === spaceId && a.workspaceMemberId === myMember.id && a.haveAccess);
    return (ea?.accessLevel as SpaceAccessLevel | undefined) ?? "None";
  }, [isAdmin, currentUser?.id, allMembers, allAccess, spaceId]);

  return {
    level,
    hasAccess: RANK[level] >= RANK.Viewer,
    canEdit:   RANK[level] >= RANK.Editor,
    canManage: RANK[level] >= RANK.Manager,
  };
}
