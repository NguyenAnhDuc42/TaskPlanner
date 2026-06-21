import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { ContentPage } from '../type';

interface WorkspaceUISettings {
  sidebarWidth: number;
  contextWidth: number;
  isInnerSidebarOpen: boolean;
}

interface WorkspaceUIState extends WorkspaceUISettings {
  hoveredIcon: ContentPage | null;
  updateSettings: (updates: Partial<WorkspaceUISettings>) => void;
  setHoveredIcon: (icon: ContentPage | null) => void;
}

const DEFAULTS: WorkspaceUISettings = {
  sidebarWidth: 250,
  contextWidth: 380,
  isInnerSidebarOpen: true,
};

export const useWorkspaceUIStore = create<WorkspaceUIState>()(
  persist(
    (set) => ({
      ...DEFAULTS,
      hoveredIcon: null,
      updateSettings: (updates) => set((state) => ({ ...state, ...updates })),
      setHoveredIcon: (icon) => set({ hoveredIcon: icon }),
    }),
    {
      name: 'user-ui-prefs',
      partialize: ({ sidebarWidth, contextWidth, isInnerSidebarOpen }) => ({
        sidebarWidth,
        contextWidth,
        isInnerSidebarOpen,
      }),
    }
  )
);
