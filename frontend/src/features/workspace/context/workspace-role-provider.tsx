import type { ReactNode } from "react";
import { WorkspaceRoleContext, useComputeWorkspaceRole } from "./use-workspace-role";

export function WorkspaceRoleProvider({ children }: Readonly<{ children: ReactNode }>) {
  const role = useComputeWorkspaceRole();
  return <WorkspaceRoleContext.Provider value={role}>{children}</WorkspaceRoleContext.Provider>;
}
