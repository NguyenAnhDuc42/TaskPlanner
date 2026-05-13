import { create } from 'zustand';
import type { WorkspaceHierarchy, FolderHierarchy, TaskHierarchy } from './hierarchy-type';

interface HierarchyState {
  hierarchy: WorkspaceHierarchy | undefined;
  folders: Record<string, FolderHierarchy[]>; // nodeId -> Folders
  tasks: Record<string, TaskHierarchy[]>; // nodeId -> Tasks
  expandedNodes: Record<string, boolean>; // nodeId -> isOpen

  setHierarchy: (hierarchy: WorkspaceHierarchy) => void;
  setFolders: (nodeId: string, folders: FolderHierarchy[]) => void;
  setTasks: (nodeId: string, tasks: TaskHierarchy[]) => void;
  updateHierarchy: (updater: (prev: WorkspaceHierarchy | undefined) => WorkspaceHierarchy | undefined) => void;
  
  toggleNodeExpand: (nodeId: string) => void;
  setNodeExpand: (nodeId: string, isOpen: boolean) => void;
}

export const useHierarchyStore = create<HierarchyState>((set) => ({
  hierarchy: undefined,
  folders: {},
  tasks: {},
  expandedNodes: {},

  setHierarchy: (hierarchy) => set({ hierarchy }),
  
  setFolders: (nodeId, folders) => set((state) => ({
    folders: { ...state.folders, [nodeId]: folders }
  })),

  setTasks: (nodeId, tasks) => set((state) => ({
    tasks: { ...state.tasks, [nodeId]: tasks }
  })),

  updateHierarchy: (updater) => set((state) => ({
    hierarchy: updater(state.hierarchy)
  })),

  toggleNodeExpand: (nodeId) => set((state) => ({
    expandedNodes: { ...state.expandedNodes, [nodeId]: !state.expandedNodes[nodeId] }
  })),

  setNodeExpand: (nodeId, isOpen) => set((state) => ({
    expandedNodes: { ...state.expandedNodes, [nodeId]: isOpen }
  })),
}));
