import { createContext, useContext } from "react";
import type { ContentPage } from "../type";

// ─── State ───────────────────────────────────────────────
export interface WorkspaceUIState {
  activeIcon: ContentPage | null;
  isInnerSidebarOpen: boolean;
  hoveredIcon: ContentPage | null;
  sidebarWidth: number;
  contextWidth: number;
}

// ─── Actions ─────────────────────────────────────────────
export interface WorkspaceUIActions {
  selectIcon: (icon: ContentPage | null) => void;
  toggleInnerSidebar: () => void;
  setHoveredIcon: (icon: ContentPage | null) => void;
  updateSidebarWidth: (width: number) => void;
  updateContextWidth: (width: number) => void;
}

// ─── Context ─────────────────────────────────────────────
export const WorkspaceSessionContext = createContext<{
  state: WorkspaceUIState;
  actions: WorkspaceUIActions;
} | null>(null);

export function useWorkspaceSession() {
  const context = useContext(WorkspaceSessionContext);
  if (!context)
    throw new Error(
      "useWorkspaceSession must be used within WorkspaceSessionProvider",
    );
  return context;
}
