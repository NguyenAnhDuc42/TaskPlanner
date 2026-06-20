import { createContext, useContext } from "react";
import type { WorkspaceRecord } from "@/types/workspace/workspace-record";
import type { ContentPage } from "../type";

export interface WorkspaceUIState {
  activeIcon: ContentPage;
  hoveredIcon: ContentPage | null;
  isInnerSidebarOpen: boolean;
  sidebarWidth: number;
  contextWidth: number;
}

export interface WorkspaceUIActions {
  toggleInnerSidebar: () => void;
  setHoveredIcon: (icon: ContentPage | null) => void;
  updateSidebarWidth: (width: number) => void;
  updateContextWidth: (width: number) => void;
  setSidebarOpenLocal: (isOpen: boolean) => void;
  setSidebarWidthLocal: (width: number) => void;
  setContextWidthLocal: (width: number) => void;
}

export interface WorkspaceContextType {
  workspaceId: string;
  workspace: WorkspaceRecord | undefined;
  isLoading: boolean;
  isError: boolean;
  error: unknown;
  ui: WorkspaceUIState;
  actions: WorkspaceUIActions;
}

export const WorkspaceContext = createContext<WorkspaceContextType | undefined>(undefined);

export function useWorkspace() {
  const context = useContext(WorkspaceContext);
  if (!context) {
    return {
      workspaceId: "",
      workspace: undefined,
      isLoading: true,
      isError: false,
      error: null,
      ui: {
        activeIcon: "projects" as const,
        hoveredIcon: null,
        isInnerSidebarOpen: true,
        sidebarWidth: 260,
        contextWidth: 350,
      },
      actions: {
        toggleInnerSidebar: () => {},
        setHoveredIcon: () => {},
        updateSidebarWidth: () => {},
        updateContextWidth: () => {},
        setSidebarOpenLocal: () => {},
        setSidebarWidthLocal: () => {},
        setContextWidthLocal: () => {},
      },
    } as WorkspaceContextType;
  }
  return context;
}

export function useWorkspaceSession() {
  const context = useContext(WorkspaceContext);

  if (!context) {
    return {
      state: {
        activeIcon: "projects" as const,
        hoveredIcon: null,
        isInnerSidebarOpen: true,
        sidebarWidth: 260,
        contextWidth: 350,
      },
      actions: {
        toggleInnerSidebar: () => {},
        setHoveredIcon: () => {},
        updateSidebarWidth: () => {},
        updateContextWidth: () => {},
        setSidebarOpenLocal: () => {},
        setSidebarWidthLocal: () => {},
        setContextWidthLocal: () => {},
      },
      workspaceId: "",
      isLoading: true,
    };
  }

  return {
    state: context.ui,
    actions: context.actions,
    workspaceId: context.workspaceId,
    isLoading: context.isLoading,
  };
}
