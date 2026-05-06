import { createContext } from "react";
import type { StatusDto } from "@/types/status";
import type { ContentPage } from "../type";
import type { WorkspaceSecurityContext } from "../api";

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
  statusMap: Record<string, StatusDto>;
  memberMap: Record<string, any>;
  workflows: any[];
}

export interface WorkspaceContextType {
  workspaceId: string;
  workspace: WorkspaceSecurityContext | undefined;
  registry: WorkspaceRegistry;
  isLoading: boolean;
  isError: boolean;
  error: any;
  ui: WorkspaceUIState;
  actions: WorkspaceUIActions;
}

export const WorkspaceContext = createContext<WorkspaceContextType | undefined>(undefined);
