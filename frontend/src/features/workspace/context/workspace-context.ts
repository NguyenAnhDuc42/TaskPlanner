import { createContext } from "react";
import type { WorkspaceSecurityContext } from "../api";
import type { WorkspaceSummary } from "@/features/main/home-screen/type";
import type { Status } from "@/types/status";
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

export interface WorkspaceRegistry {
  statusMap: Record<string, Status>;
  memberMap: Record<string, any>;
  workflows: any[];
}

export interface WorkspaceContextType {
  workspaceId: string;
  workspace: WorkspaceSecurityContext | undefined;
  workspaces: WorkspaceSummary[];
  registry: WorkspaceRegistry;
  isLoading: boolean;
  isError: boolean;
  error: any;
  ui: WorkspaceUIState;
  actions: WorkspaceUIActions;
}

export const WorkspaceContext = createContext<WorkspaceContextType | undefined>(undefined);
