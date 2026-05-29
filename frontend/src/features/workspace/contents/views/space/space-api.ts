import { workspaceApi } from "@/store/workspaceApi";
import { spaceSlice, folderSlice, taskSlice, statusSlice, folderSelectors, taskSelectors, statusSelectors } from "@/store/entityStore";
import { useSelector } from "react-redux";
import { createSelector } from "@reduxjs/toolkit";
import { useMemo } from "react";
import type { RootState } from "@/store";
import type { SpaceRecord, FolderRecord, TaskRecord } from "@/types/projects";
import type { Status } from "@/types/status";
import { StatusCategory } from "@/types/status-category";


export interface SpaceItemsResponse {
  folders: FolderRecord[];
  tasks: TaskRecord[];
  statuses: Status[];
}

export interface BatchUpdateSpaceItemValue {
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

    batchUpdateSpaceItems: build.mutation<void, { spaceId: string; updates: BatchUpdateSpaceItemValue[] }>({
      query: ({ spaceId, updates }) => ({
        url: "/spaces/batch-update",
        method: "POST",
        data: { workspaceId: spaceId, updates }
      }),
      async onQueryStarted({ spaceId, updates }, { dispatch, queryFulfilled, getState }) {
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

        // 🔥 OPTIMISTIC QUERY CACHE UPDATE: Immediately patch active query data to prevent rebound
        const patchResult = dispatch(
          spaceApi.util.updateQueryData("getSpaceItems", spaceId, (draft) => {
            if (!draft) return;
            updates.forEach((u) => {
              if (u.type === "ProjectFolder") {
                const folder = draft.folders.find((f) => f.id === u.id);
                if (folder) {
                  if (u.statusId !== undefined) folder.statusId = u.statusId ?? undefined;
                  if (u.priority !== undefined) folder.priority = (u.priority ?? undefined) as any;
                  if (u.orderKey !== undefined) folder.orderKey = u.orderKey ?? undefined;
                }
              } else {
                const task = draft.tasks.find((t) => t.id === u.id);
                if (task) {
                  if (u.statusId !== undefined) task.statusId = u.statusId ?? undefined;
                  if (u.priority !== undefined) task.priority = (u.priority ?? undefined) as any;
                  if (u.orderKey !== undefined) task.orderKey = u.orderKey ?? undefined;
                }
              }
            });
          })
        );

        try {
          await queryFulfilled;
        } catch {
          // Rollback on failure
          patchResult.undo();
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

const statusCategoryWeight = {
  [StatusCategory.NotStarted]: 0,
  [StatusCategory.Active]: 1,
  [StatusCategory.Done]: 2,
  [StatusCategory.Closed]: 3,
};

export function useSpaceStatuses(spaceId: string) {
  const space = useSpaceDetail(spaceId);
  const selectSpaceStatuses = useMemo(() =>
    createSelector(
      [statusSelectors.selectAll],
      (statuses: Status[]) => {
        const targetWorkflowId = space?.workflowId;
        if (!targetWorkflowId) return [];
        return statuses
          .filter(s => s.workflowId === targetWorkflowId)
          .sort((a, b) => {
            const weightA = statusCategoryWeight[a.category] ?? 4;
            const weightB = statusCategoryWeight[b.category] ?? 4;
            if (weightA !== weightB) return weightA - weightB;
            return (a.orderKey || "").localeCompare(b.orderKey || "");
          });
      }
    ),
  [space?.workflowId]);

  return useSelector(selectSpaceStatuses);
}
