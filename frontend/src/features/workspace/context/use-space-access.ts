import { useSelector } from "react-redux";
import { entityAccessSelectors } from "@/store/entityStore";
import { useWorkspaceRole } from "./use-workspace-role";
import type { RootState } from "@/store";

export function useSpaceAccess(spaceId: string) {
  const { isAdmin } = useWorkspaceRole();

  const entityAccess = useSelector((state: RootState) =>
    entityAccessSelectors.selectAll(state).find(
      ea => ea.spaceId === spaceId && ea.haveAccess
    )
  );

  // Admins and owners bypass entity access entirely
  if (isAdmin) {
    return { canView: true, canEdit: true, canManage: true };
  }

  const level = entityAccess?.accessLevel;

  return {
    canView:   !!level,
    canEdit:   level === "Editor"  || level === "Manager",
    canManage: level === "Manager",
  };
}
