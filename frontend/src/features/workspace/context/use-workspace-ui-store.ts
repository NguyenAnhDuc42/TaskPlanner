import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { ContentPage } from '../type';

interface WorkspaceUISettings {
  sidebarWidth: number;
  contextWidth: number;
  isInnerSidebarOpen: boolean;
}

interface WorkspaceUIState {
  settings: Record<string, WorkspaceUISettings>;
  hoveredIcon: ContentPage | null;
  
  // Actions
  getSettings: (workspaceId: string) => WorkspaceUISettings;
  updateSettings: (workspaceId: string, updates: Partial<WorkspaceUISettings>) => void;
  setHoveredIcon: (icon: ContentPage | null) => void;
}

const DEFAULT_SETTINGS: WorkspaceUISettings = {
  sidebarWidth: 260,
  contextWidth: 350,
  isInnerSidebarOpen: true,
};

export const useWorkspaceUIStore = create<WorkspaceUIState>()(
  persist(
    (set, get) => ({
      settings: {},
      hoveredIcon: null,

      getSettings: (workspaceId) => {
        return get().settings[workspaceId] || DEFAULT_SETTINGS;
      },

      updateSettings: (workspaceId, updates) => set((state) => {
        const current = state.settings[workspaceId] || DEFAULT_SETTINGS;
        return {
          settings: {
            ...state.settings,
            [workspaceId]: { ...current, ...updates },
          },
        };
      }),

      setHoveredIcon: (icon) => set({ hoveredIcon: icon }),
    }),
    {
      name: 'workspace-ui-storage',
      partialize: (state) => ({ settings: state.settings }), // Only persist settings, not hovers
    }
  )
);
