import { createContext, useContext, type ReactNode } from "react";
import { useWorkspaceDetail, useWorkspaceStatuses, type WorkspaceSecurityContext } from "../api";
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
    isLoading: isWorkspaceLoading, 
    error: workspaceError 
  } = useWorkspaceDetail(workspaceId);

  const { 
    data: statuses = [], 
    isLoading: isStatusesLoading, 
    error: statusesError 
  } = useWorkspaceStatuses();

  const isLoading = isWorkspaceLoading || isStatusesLoading;
  const error = workspaceError || statusesError;

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
