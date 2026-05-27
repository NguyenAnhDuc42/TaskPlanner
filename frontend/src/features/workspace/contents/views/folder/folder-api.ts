import { workspaceApi } from "@/store/workspaceApi";
import { folderSlice, taskSlice, statusSlice, taskSelectors, folderSelectors } from "@/store/entityStore";
import { useSelector } from "react-redux";
import { createSelector } from "@reduxjs/toolkit";
import { useMemo } from "react";
import type { RootState } from "@/store";
import type { TaskRecord } from "@/types/projects/task-record";
import type { FolderRecord } from "@/types/projects/folder-record";
import type { PagedResult } from "@/types/paged-result";
import { Priority } from "@/types/priority";
import type { Status } from "@/types/status";

export interface FolderDetailResponse {
  folder: FolderRecord;
  statuses: Status[];
  workflowId?: string;
}

export interface TaskFilter {
  statusIds?: string[];
  priorities?: string[];
  assigneeIds?: string[];
  startDate?: string;
  dueDate?: string;
  search?: string;
}

export interface BatchUpdateFolderTaskValue {
  id: string;
  statusId?: string | null;
  priority?: Priority | null;
  startDate?: string | null;
  dueDate?: string | null;
  orderKey?: string | null;
  isDeleted?: boolean | null;
}

// 1. Inject Folder endpoints directly into our central base query (100% Type-Safe)
export const folderApi = workspaceApi.injectEndpoints({
  endpoints: (build) => ({
    getFolderDetail: build.query<FolderDetailResponse, string>({
      query: (folderId) => ({ url: `/folders/${folderId}`, method: 'GET' }),
      async onQueryStarted(folderId, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(folderSlice.actions.upsert(data.folder));
          dispatch(statusSlice.actions.upsertMany(data.statuses));
        } catch {}
      }
    }),

    getFolderTasks: build.query<PagedResult<TaskRecord>, { folderId: string; cursor?: string | null; filter?: TaskFilter }>({
      query: ({ folderId, cursor, filter }) => ({
        url: `/folders/${folderId}/tasks`,
        method: 'POST',
        data: { cursor, limit: 50, filter }
      }),
      async onQueryStarted(_, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(taskSlice.actions.upsertMany(data.items));
        } catch {}
      }
    }),

    batchUpdateFolderTasks: build.mutation<void, { folderId: string; updates: BatchUpdateFolderTaskValue[] }>({
      query: ({ folderId, updates }) => ({
        url: `/folders/${folderId}/tasks/batch`,
        method: 'POST',
        data: { folderId, updates }
      }),
      async onQueryStarted({ updates }, { dispatch, queryFulfilled, getState }) {
        const state = getState() as RootState;
        
        // 1. Snapshot ONLY the specific original keys of the target tasks for type-safe granular rollback
        const originalTasks = updates
          .map((u) => taskSelectors.selectById(state, u.id))
          .filter((t): t is TaskRecord => !!t);

        // 2. Perform Optimistic Update instantly on Redux
        dispatch(taskSlice.actions.upsertMany(updates as Partial<TaskRecord>[]));

        try {
          await queryFulfilled;
        } catch {
          // 3. Rollback on failure
          dispatch(taskSlice.actions.upsertMany(originalTasks));
        }
      }
    }),

    updateFolderField: build.mutation<void, { folderId: string; patches: Partial<FolderRecord> }>({
      query: ({ folderId, patches }) => ({
        url: `/folders/${folderId}`,
        method: 'PATCH',
        data: patches
      }),
      async onQueryStarted({ folderId, patches }, { dispatch, queryFulfilled, getState }) {
        const state = getState() as RootState;
        const originalFolder = folderSelectors.selectById(state, folderId);

        // 1. Optimistic update
        dispatch(folderSlice.actions.upsert({ id: folderId, ...patches }));

        try {
          await queryFulfilled;
        } catch {
          // 2. Rollback on failure
          if (originalFolder) {
            dispatch(folderSlice.actions.upsert(originalFolder));
          }
        }
      }
    })
  })
});

// Export Hooks
export const {
  useGetFolderDetailQuery,
  useGetFolderTasksQuery,
  useBatchUpdateFolderTasksMutation,
  useUpdateFolderFieldMutation,
} = folderApi;

// Convenience wrapper — pre-binds folderId so call-sites just pass updates[]
export function useBatchUpdateFolderTasks(folderId: string) {
  const [mutate, result] = useBatchUpdateFolderTasksMutation();
  return {
    ...result,
    mutate: (updates: BatchUpdateFolderTaskValue[]) => mutate({ folderId, updates }),
  };
}

// --- READ CUSTOM SELECTORS (Hooks components call directly to query central tables) ---
export function useFolderDetail(folderId: string) {
  return useSelector((state: RootState) => folderSelectors.selectById(state, folderId));
}

export function useFolderTasksList(folderId: string) {
  const selectTasksForFolder = useMemo(() =>
    createSelector(
      [taskSelectors.selectAll],
      (tasks) => tasks.filter((t): t is TaskRecord => t.projectFolderId === folderId)
    ),
  [folderId]);

  return useSelector(selectTasksForFolder);
}
