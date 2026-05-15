import { create } from 'zustand';
import type { Status } from '@/types/status';
import type { MemberSummary } from '@/features/workspace/contents/members/members-type';

interface WorkspaceDataState {
  statuses: Record<string, Status>; // statusId -> Status
  spaceStatuses: Record<string, string[]>; // spaceId -> statusIds
  folderStatuses: Record<string, string[]>; // folderId -> statusIds
  members: Record<string, MemberSummary>; // memberId -> Member
  
  // Actions
  setStatuses: (statuses: Status[]) => void;
  setSpaceStatuses: (spaceId: string, statusIds: string[]) => void;
  setFolderStatuses: (folderId: string, statusIds: string[]) => void;
  setMembers: (members: MemberSummary[]) => void;
}

export const useWorkspaceDataStore = create<WorkspaceDataState>((set) => ({
  statuses: {},
  spaceStatuses: {},
  folderStatuses: {},
  members: {},

  setStatuses: (statuses) => set((state) => {
    const newStatuses = { ...state.statuses };
    statuses.forEach(s => {
      newStatuses[s.statusId] = s;
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
}));
