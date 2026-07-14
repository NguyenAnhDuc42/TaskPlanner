import type { ReactNode } from "react";
import { WorkspaceRoleContext, useComputeWorkspaceRole } from "./use-workspace-role";

// Must sit inside WorkspaceProvider — the role derives from useWorkspace().
export function WorkspaceRoleProvider({ children }: Readonly<{ children: ReactNode }>) {
  const role = useComputeWorkspaceRole();
  return <WorkspaceRoleContext.Provider value={role}>{children}</WorkspaceRoleContext.Provider>;
}
