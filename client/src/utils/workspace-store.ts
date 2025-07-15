import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface WorkspaceState {
  selectedWorkspaceId: string | undefined;
  setSelectedWorkspaceId: (id: string | undefined) => void;
}

export const useWorkspaceStore = create<WorkspaceState>()(
  persist(
    (set) => ({
      selectedWorkspaceId: undefined,
      setSelectedWorkspaceId: (id) => set({ selectedWorkspaceId: id }),
    }),
    {
      name: 'workspace-selection',
      partialize: (state) => ({ selectedWorkspaceId: state.selectedWorkspaceId }),
    }
  )
);