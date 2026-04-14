import { createContext, useContext, type ReactNode } from "react";
import { useWorkspaceDetail, type WorkspaceSecurityContext } from "../api";
import type { StatusDto } from "../contents/hierarchy/views/views-type";

interface WorkspaceContextType {
  workspaceId: string;
  workspace: WorkspaceSecurityContext | undefined;
  statuses: StatusDto[];
  isLoading: boolean;
  error: any;
}

const WorkspaceContext = createContext<WorkspaceContextType | undefined>(undefined);

export function useWorkspace() {
  const context = useContext(WorkspaceContext);
  if (!context) {
    throw new Error("useWorkspace must be used within a WorkspaceProvider");
  }
  return context;
}

interface WorkspaceProviderProps {
  workspaceId: string;
  children: ReactNode;
}

export function WorkspaceProvider({ workspaceId, children }: WorkspaceProviderProps) {
  const { 
    data: workspace, 
    isLoading, 
    error 
  } = useWorkspaceDetail(workspaceId);

  // Statuses are now resolved at the Space/Folder level 
  const statuses: StatusDto[] = [];

  return (
    <WorkspaceContext.Provider
      value={{
        workspaceId,
        workspace,
        statuses,
        isLoading,
        error,
      }}
    >
      {children}
    </WorkspaceContext.Provider>
  );
}
