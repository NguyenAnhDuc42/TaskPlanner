import { create } from 'zustand';
import type { SpaceRecord, FolderRecord, TaskRecord } from '@/types/projects';

interface HierarchyState {
  // 1. FLAT DICTIONARIES (The Single Source of Truth)
  spaces: Record<string, SpaceRecord>;
  folders: Record<string, FolderRecord>;
  tasks: Record<string, TaskRecord>;

  // 2. RELATIONSHIPS (Arrays of IDs for ordering)
  rootSpaceIds: string[]; 
  foldersBySpace: Record<string, string[]>; // spaceId -> folderIds
  tasksByParent: Record<string, string[]>;  // parentId -> taskIds

  // 3. UI STATE
  expandedNodes: Record<string, boolean>; // nodeId -> isOpen
  
  // ACTIONS
  setSpaces: (spaces: SpaceRecord[]) => void;
  setFolders: (spaceId: string, folders: FolderRecord[]) => void;
  setTasks: (parentId: string, tasks: TaskRecord[]) => void;
  
  toggleNodeExpand: (nodeId: string) => void;
  setNodeExpand: (nodeId: string, isOpen: boolean) => void;
}

export const useHierarchyStore = create<HierarchyState>((set) => ({
  spaces: {},
  folders: {},
  tasks: {},

  rootSpaceIds: [],
  foldersBySpace: {},
  tasksByParent: {},

  expandedNodes: {},

  setSpaces: (spaces) => set((state) => {
    const newSpaces = { ...state.spaces };
    const newRootSpaceIds = [...state.rootSpaceIds];
    
    spaces.forEach(s => {
      newSpaces[s.id] = s;
      if (!newRootSpaceIds.includes(s.id)) {
        newRootSpaceIds.push(s.id);
      }
    });

    return { 
      spaces: newSpaces,
      rootSpaceIds: newRootSpaceIds 
    };
  }),

  setFolders: (spaceId, folders) => set((state) => {
    const newFolders = { ...state.folders };
    const folderIds = state.foldersBySpace[spaceId] ? [...state.foldersBySpace[spaceId]] : [];
    
    folders.forEach(f => {
      newFolders[f.id] = f;
      if (!folderIds.includes(f.id)) {
        folderIds.push(f.id);
      }
    });

    return { 
      folders: newFolders,
      foldersBySpace: { ...state.foldersBySpace, [spaceId]: folderIds }
    };
  }),

  setTasks: (parentId, tasks) => set((state) => {
    const newTasks = { ...state.tasks };
    const taskIds = state.tasksByParent[parentId] ? [...state.tasksByParent[parentId]] : [];
    
    tasks.forEach(t => {
      newTasks[t.id] = t;
      if (!taskIds.includes(t.id)) {
        taskIds.push(t.id);
      }
    });

    return { 
      tasks: newTasks,
      tasksByParent: { ...state.tasksByParent, [parentId]: taskIds }
    };
  }),

  toggleNodeExpand: (nodeId) => set((state) => ({
    expandedNodes: { ...state.expandedNodes, [nodeId]: !state.expandedNodes[nodeId] }
  })),

  setNodeExpand: (nodeId, isOpen) => set((state) => ({
    expandedNodes: { ...state.expandedNodes, [nodeId]: isOpen }
  })),
}));
