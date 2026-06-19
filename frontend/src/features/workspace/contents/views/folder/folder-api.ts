import { workspaceApi } from "@/store/workspaceApi";
import { folderSlice, taskSlice, statusSlice, taskSelectors, folderSelectors, statusSelectors } from "@/store/entityStore";
import { useSelector } from "react-redux";
import { createSelector } from "@reduxjs/toolkit";
import { useMemo } from "react";
import type { RootState } from "@/store";
import type { TaskRecord } from "@/types/projects/task-record";
import type { FolderRecord } from "@/types/projects/folder-record";
import type { PagedResult } from "@/types/paged-result";
import { Priority } from "@/types/priority";
import type { Status } from "@/types/status";
import { StatusCategory } from "@/types/status-category";
import type { BreadcrumbInfo } from "@/types/breadcrumb-info";
import { toast } from "sonner";


export interface FolderDetailResponse {
  folder: FolderRecord;
  space: BreadcrumbInfo;
  folderStatus?: Status;
  parentWorkflowId?: string; 
  spaceStatuses: Status[]; 
  taskStatuses: Status[]; 
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
  clearStartDate?: boolean;
  clearDueDate?: boolean;
}

export const folderApi = workspaceApi.injectEndpoints({
  overrideExisting: true,
  endpoints: (build) => ({
    getFolderDetail: build.query<FolderDetailResponse, string>({
      query: (folderId) => ({ url: `/folders/${folderId}`, method: 'GET' }),
      async onQueryStarted(folderId, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(folderSlice.actions.upsert({ ...data.folder, workflowId: data.workflowId ?? undefined }));
          dispatch(statusSlice.actions.upsertMany(data.spaceStatuses));
          dispatch(statusSlice.actions.upsertMany(data.taskStatuses));
        } catch (error) {
          console.error(`[folderApi] Failed to fetch details for folder ${folderId}:`, error);
        }
      }
    }),

    getFolderTasks: build.query<PagedResult<TaskRecord>, { folderId: string; cursor?: string | null; filter?: TaskFilter }>({
      query: ({ folderId, cursor, filter }) => ({
        url: `/folders/${folderId}/tasks`,
        method: 'POST',
        data: { cursor, limit: 15, filter }
      }),
      serializeQueryArgs: ({ queryArgs }) => {
        const { cursor: _, ...rest } = queryArgs;
        return rest;
      },
      merge: (currentCache, newItems) => {
        if (newItems.items) {
          // ensure no duplicates
          const existingIds = new Set(currentCache.items.map(i => i.id));
          const newUnique = newItems.items.filter(i => !existingIds.has(i.id));
          currentCache.items.push(...newUnique);
        }
        currentCache.nextCursor = newItems.nextCursor;
        currentCache.hasNextPage = newItems.hasNextPage;
      },
      forceRefetch({ currentArg, previousArg }) {
        return currentArg?.cursor !== previousArg?.cursor;
      },
      async onQueryStarted({ folderId }, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(taskSlice.actions.upsertMany(data.items));
        } catch (error) {
          console.error(`[folderApi] Failed to fetch tasks for folder ${folderId}:`, error);
        }
      }
    }),

    batchUpdateFolderTasks: build.mutation<void, { folderId: string; updates: BatchUpdateFolderTaskValue[] }>({
      query: ({ folderId, updates }) => ({
        url: `/folders/${folderId}/tasks/batch`,
        method: 'PUT',
        data: { folderId, updates }
      }),
      async onQueryStarted({ updates }, { dispatch, queryFulfilled, getState }) {
        const state = getState() as RootState;
        
        // 1. Snapshot ONLY the specific original keys of the target tasks for type-safe granular rollback
        const originalTasks = updates
          .map((u) => taskSelectors.selectById(state, u.id))
          .filter((t): t is TaskRecord => !!t);

        // 2. Perform Optimistic Update instantly on Redux
        const optimisticUpdates = updates.map(u => ({
          ...u,
          ...(u.clearStartDate ? { startDate: null } : {}),
          ...(u.clearDueDate ? { dueDate: null } : {}),
        }));
        dispatch(taskSlice.actions.upsertMany(optimisticUpdates as Partial<TaskRecord>[]));

        try {
          await queryFulfilled;
        } catch {
          if (originalTasks.length > 0) {
            dispatch(taskSlice.actions.upsertMany(originalTasks));
          }
          toast.error("Failed to update tasks. Your changes have been reverted.");
        }
      }
    }),

    updateFolderField: build.mutation<void, { folderId: string; patches: Partial<FolderRecord> & { clearStartDate?: boolean; clearDueDate?: boolean; clearStatusId?: boolean; clearPriority?: boolean } }>({
      query: ({ folderId, patches }) => ({
        url: `/folders/${folderId}`,
        method: 'PUT',
        data: patches
      }),
      async onQueryStarted({ folderId, patches }, { dispatch, queryFulfilled, getState }) {
        const state = getState() as RootState;
        const originalFolder = folderSelectors.selectById(state, folderId);

        // 1. Optimistic update
        const optimisticUpdates: Partial<FolderRecord> = { id: folderId, ...patches };
        if (patches.clearStartDate) optimisticUpdates.startDate = null as unknown as undefined;
        if (patches.clearDueDate) optimisticUpdates.dueDate = null as unknown as undefined;
        if (patches.clearStatusId) optimisticUpdates.statusId = null as unknown as undefined;
        if (patches.clearPriority) optimisticUpdates.priority = null as unknown as undefined;

        dispatch(folderSlice.actions.upsert(optimisticUpdates as FolderRecord));

        try {
          await queryFulfilled;
        } catch {
          if (originalFolder) {
            dispatch(folderSlice.actions.upsert(originalFolder));
          }
          toast.error("Failed to update folder. Your changes have been reverted.");
        }
      }
    })
  })
});


export const {
  useGetFolderDetailQuery,
  useGetFolderTasksQuery,
  useBatchUpdateFolderTasksMutation,
  useUpdateFolderFieldMutation,
} = folderApi;

export function useBatchUpdateFolderTasks(folderId: string) {
  const [mutate, result] = useBatchUpdateFolderTasksMutation();
  return {
    ...result,
    mutate: (updates: BatchUpdateFolderTaskValue[]) => mutate({ folderId, updates }),
  };
}

export function useFolderDetail(folderId: string) {
  return useSelector((state: RootState) => folderSelectors.selectById(state, folderId));
}

export function useFolderTasksList(folderId: string) {
  const selectTasksForFolder = useMemo(() =>
    createSelector(
      [taskSelectors.selectAll],
      (tasks) => tasks
        .filter((t): t is TaskRecord => t.folderId === folderId && !t.parentTaskId)
        .sort((a, b) => ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1))
    ),
  [folderId]);

  return useSelector(selectTasksForFolder);
}

const statusCategoryWeight = {
  [StatusCategory.NotStarted]: 0,
  [StatusCategory.Active]: 1,
  [StatusCategory.Done]: 2,
  [StatusCategory.Closed]: 3,
};

export function useFolderStatuses(folderId: string) {
  const folder = useFolderDetail(folderId);
  const selectFolderStatuses = useMemo(() =>
    createSelector(
      [statusSelectors.selectAll],
      (statuses: Status[]) => {
        const targetWorkflowId = folder?.workflowId;
        if (!targetWorkflowId) return [];
        return statuses
          .filter(s => s.workflowId?.toLowerCase() === targetWorkflowId.toLowerCase())
          .sort((a, b) => {
            const weightA = statusCategoryWeight[a.category] ?? 4;
            const weightB = statusCategoryWeight[b.category] ?? 4;
            if (weightA !== weightB) return weightA - weightB;
            return ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1);
          });
      }
    ),
  [folder?.workflowId]);

  return useSelector(selectFolderStatuses);
}
