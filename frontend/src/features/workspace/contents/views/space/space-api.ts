import { workspaceApi } from "@/store/workspaceApi";
import { spaceSlice, folderSlice, taskSlice, statusSlice, folderSelectors, taskSelectors } from "@/store/entityStore";
import { useSelector } from "react-redux";
import { createSelector } from "@reduxjs/toolkit";
import { useMemo } from "react";
import type { RootState } from "@/store";
import type { SpaceRecord, FolderRecord, TaskRecord } from "@/types/projects";
import type { Status } from "@/types/status";
import { prioritySort } from "@/types/priority";

export interface SpaceItemsResponse {
  folders: FolderRecord[];
  tasks: TaskRecord[];
  statuses: Status[];
}

export interface SpaceBatchUpdateValue {
  id: string;
  type: "ProjectFolder" | "ProjectTask";
  statusId?: string | null;
  priority?: string | null;
  orderKey?: string | null;
  previousItemOrderKey?: string | null;
  nextItemOrderKey?: string | null;
}

export const spaceApi = workspaceApi.injectEndpoints({
  endpoints: (build) => ({
    getSpaceDetail: build.query<SpaceRecord, string>({
      query: (spaceId) => ({ url: `/spaces/${spaceId}`, method: "GET" }),
      async onQueryStarted(spaceId, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(spaceSlice.actions.upsert(data));
        } catch {}
      }
    }),

    getSpaceItems: build.query<SpaceItemsResponse, string>({
      query: (spaceId) => ({ url: `/spaces/${spaceId}/items`, method: "GET" }),
      async onQueryStarted(spaceId, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(folderSlice.actions.upsertMany(data.folders));
          dispatch(taskSlice.actions.upsertMany(data.tasks));
          dispatch(statusSlice.actions.upsertMany(data.statuses));
        } catch {}
      }
    }),

    batchUpdateSpaceItems: build.mutation<void, { spaceId: string; updates: SpaceBatchUpdateValue[] }>({
      query: ({ spaceId, updates }) => ({
        url: "/spaces/batch-update",
        method: "POST",
        data: { workspaceId: spaceId, updates }
      }),
      async onQueryStarted({ updates }, { dispatch, queryFulfilled, getState }) {
        const state = getState() as RootState;

        // Snapshot original folders and tasks for rollback
        const originalFolders: FolderRecord[] = [];
        const originalTasks: TaskRecord[] = [];

        updates.forEach((u) => {
          if (u.type === "ProjectFolder") {
            const folder = folderSelectors.selectById(state, u.id);
            if (folder) originalFolders.push(folder);
          } else {
            const task = taskSelectors.selectById(state, u.id);
            if (task) originalTasks.push(task);
          }
        });

        // Optimistically update standard status fields on folders & tasks
        const folderUpdates = updates.filter((u) => u.type === "ProjectFolder");
        const taskUpdates = updates.filter((u) => u.type === "ProjectTask");

        if (folderUpdates.length > 0) {
          dispatch(folderSlice.actions.upsertMany(folderUpdates as Partial<FolderRecord>[]));
        }
        if (taskUpdates.length > 0) {
          dispatch(taskSlice.actions.upsertMany(taskUpdates as Partial<TaskRecord>[]));
        }

        try {
          await queryFulfilled;
        } catch {
          // Rollback on failure
          if (originalFolders.length > 0) {
            dispatch(folderSlice.actions.upsertMany(originalFolders));
          }
          if (originalTasks.length > 0) {
            dispatch(taskSlice.actions.upsertMany(originalTasks));
          }
        }
      }
    }),

    updateSpaceField: build.mutation<void, { spaceId: string; patches: Partial<SpaceRecord> }>({
      query: ({ spaceId, patches }) => ({
        url: `/spaces/${spaceId}`,
        method: "PUT",
        data: patches
      }),
      async onQueryStarted({ spaceId, patches }, { dispatch, queryFulfilled, getState }) {
        const state = getState() as RootState;
        const originalSpace = state.spaces.entities[spaceId];

        dispatch(spaceSlice.actions.upsert({ id: spaceId, ...patches }));

        try {
          await queryFulfilled;
        } catch {
          if (originalSpace) {
            dispatch(spaceSlice.actions.upsert(originalSpace));
          }
        }
      }
    })
  })
});

export const {
  useGetSpaceDetailQuery,
  useGetSpaceItemsQuery,
  useBatchUpdateSpaceItemsMutation,
  useUpdateSpaceFieldMutation,
} = spaceApi;

// Selectors
export function useSpaceDetail(spaceId: string) {
  return useSelector((state: RootState) => state.spaces.entities[spaceId]);
}

export type BoardItem =
  | (FolderRecord & { __type: "folder" })
  | (TaskRecord & { __type: "task" });

export function useSpaceBoardItems(spaceId: string) {
  const selectBoardItemsForSpace = useMemo(() =>
    createSelector(
      [folderSelectors.selectAll, taskSelectors.selectAll],
      (folders, tasks) => {
        const targetSpaceId = spaceId.toLowerCase();

        const spaceFolders = folders
          .filter((f) => f.spaceId?.toLowerCase() === targetSpaceId)
          .map((f) => ({
            ...f,
            __type: "folder" as const,
          }));

        const spaceTasks = tasks
          .filter((t) => t.projectSpaceId?.toLowerCase() === targetSpaceId && !t.projectFolderId)
          .map((t) => ({
            ...t,
            __type: "task" as const,
          }));

        return [...spaceFolders, ...spaceTasks] as BoardItem[];
      }
    ),
  [spaceId]);

  return useSelector(selectBoardItemsForSpace);
}
