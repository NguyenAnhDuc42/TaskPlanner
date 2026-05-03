import { createContext, useContext, useEffect, type ReactNode } from "react";
import { useNavigate } from "@tanstack/react-router";
import { useWorkspaceDetail, type WorkspaceSecurityContext } from "../api";
import { useUpdateUserPreference } from "@/features/main/user-preference-api";
import { signalRService } from "@/lib/signalr-service";
import type { StatusDto } from "@/types/status";

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
  const navigate = useNavigate();
  const {
    data: workspace,
    isLoading,
    error,
    isError,
  } = useWorkspaceDetail(workspaceId);
  const { mutate: updatePreferences } = useUpdateUserPreference();

  // Save this as the user's last active workspace
  useEffect(() => {
    if (workspace && !isError) {
      updatePreferences({ lastWorkspaceId: workspaceId });
    }
  }, [workspaceId, workspace, isError, updatePreferences]);

  // Handle edge case: workspace doesn't exist or user got kicked
  useEffect(() => {
    if (isError && error) {
      const status = (error as any)?.response?.status;
      if (status === 403 || status === 404) {
        console.warn(`[WorkspaceProvider] Access denied or workspace not found (${status}). Redirecting to home.`);
        // Clear lastWorkspaceId so we don't redirect back here
        updatePreferences({ clearLastWorkspaceId: true } as any);
        navigate({ to: "/" });
      }
    }
  }, [isError, error, navigate, updatePreferences]);

  useEffect(() => {
    const manageConnection = async () => {
      try {
        await signalRService.startConnection();
        await signalRService.invoke("JoinWorkspace", workspaceId);
      } catch (err) {
        console.error("[SignalR] Failed to join workspace group:", err);
      }
    };

    manageConnection();

    return () => {
      signalRService.invoke("LeaveWorkspace", workspaceId).catch(() => {});
    };
  }, [workspaceId]);

  // Statuses are now resolved at the Space/Folder level
  const statuses: StatusDto[] = [];

  // Don't render children if access denied
  if (isError) {
    const status = (error as any)?.response?.status;
    if (status === 403 || status === 404) {
      return null;
    }
  }

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
