import { create } from 'zustand';
import type { StatusDto } from '@/types/status';
import type { MemberSummary } from '@/features/workspace/contents/members/members-type';

interface RegistryState {
  // Global lookups for helper data
  statuses: Record<string, StatusDto>; // statusId -> Status
  spaceStatuses: Record<string, string[]>; // spaceId -> statusIds
  folderStatuses: Record<string, string[]>; // folderId -> statusIds
  members: Record<string, MemberSummary>; // memberId -> Member
  
  // Tree state for fast sidebar operations
  expandedNodes: Record<string, boolean>; // nodeId -> isOpen
  
  // Actions
  setStatuses: (statuses: StatusDto[]) => void;
  setSpaceStatuses: (spaceId: string, statusIds: string[]) => void;
  setFolderStatuses: (folderId: string, statusIds: string[]) => void;
  setMembers: (members: MemberSummary[]) => void;
  toggleNodeExpand: (nodeId: string) => void;
  setNodeExpand: (nodeId: string, isOpen: boolean) => void;
}

export const useRegistryStore = create<RegistryState>((set) => ({
  statuses: {},
  spaceStatuses: {},
  folderStatuses: {},
  members: {},
  expandedNodes: {},

  setStatuses: (statuses) => set((state) => {
    const newStatuses = { ...state.statuses };
    statuses.forEach(s => {
      newStatuses[s.id] = s;
    });
    return { statuses: newStatuses };
  }),

  setSpaceStatuses: (spaceId, statusIds) => set((state) => ({
    spaceStatuses: { ...state.spaceStatuses, [spaceId]: statusIds }
  })),

  setFolderStatuses: (folderId, statusIds) => set((state) => ({
    folderStatuses: { ...state.folderStatuses, [folderId]: statusIds }
  })),

  setMembers: (members) => set((state) => {
    const newMembers = { ...state.members };
    members.forEach(m => {
      newMembers[m.id] = m;
    });
    return { members: newMembers };
  }),

  toggleNodeExpand: (nodeId) => set((state) => ({
    expandedNodes: { ...state.expandedNodes, [nodeId]: !state.expandedNodes[nodeId] }
  })),

  setNodeExpand: (nodeId, isOpen) => set((state) => ({
    expandedNodes: { ...state.expandedNodes, [nodeId]: isOpen }
  })),
}));
