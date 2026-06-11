import { createContext } from "react";
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
  error: any;
  ui: WorkspaceUIState;
  actions: WorkspaceUIActions;
}

export const WorkspaceContext = createContext<WorkspaceContextType | undefined>(undefined);
