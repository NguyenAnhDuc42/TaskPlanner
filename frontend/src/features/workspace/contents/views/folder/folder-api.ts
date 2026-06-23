import { workspaceApi } from "@/store/workspaceApi";
import { folderSlice, taskSlice, statusSlice, taskSelectors, folderSelectors, statusSelectors } from "@/store/entityStore";
import { store } from "@/store";
import { useSelector } from "react-redux";
import { createSelector } from "@reduxjs/toolkit";
import { useMemo, useRef, useEffect, useLayoutEffect, useCallback } from "react";
import type { TaskRecord } from "@/types/projects/task-record";
import type { RootState } from "@/store";
import type { TaskRecord } from "@/types/projects/task-record";
import type { FolderRecord } from "@/types/projects/folder-record";
import type { PagedResult } from "@/types/paged-result";
import { Priority } from "@/types/priority";
import type { Status } from "@/types/status";
import { StatusCategory } from "@/types/status-category";
import type { BreadcrumbInfo } from "@/types/breadcrumb-info";
import { toast } from "sonner";
import { extractErrorMessage } from "@/types/api-error";


export interface FolderDetailResponse {
  folder: FolderRecord;
  space: BreadcrumbInfo;
  spaceStatuses: Status[];
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
          dispatch(folderSlice.actions.upsert(data.folder));
          dispatch(statusSlice.actions.upsertMany(data.spaceStatuses));
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
        const optimisticUpdates = updates
          .filter(u => !u.isDeleted)
          .map(u => ({
            ...u,
            ...(u.clearStartDate ? { startDate: null } : {}),
            ...(u.clearDueDate ? { dueDate: null } : {}),
          }));
          
        const deletedIds = updates
          .filter(u => u.isDeleted)
          .map(u => u.id);

        if (optimisticUpdates.length > 0) {
          dispatch(taskSlice.actions.upsertMany(optimisticUpdates as Partial<TaskRecord>[]));
        }
        if (deletedIds.length > 0) {
          dispatch(taskSlice.actions.removeMany(deletedIds));
        }

        try {
          await queryFulfilled;
        } catch (err) {
          if (originalTasks.length > 0) {
            dispatch(taskSlice.actions.upsertMany(originalTasks));
          }
          toast.error(extractErrorMessage(err, "Failed to update tasks. Your changes have been reverted."));
        }
      }
    }),

    updateFolderField: build.mutation<void, { folderId: string; patches: Partial<FolderRecord> & { clearStartDate?: boolean; clearDueDate?: boolean } }>({
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

        dispatch(folderSlice.actions.upsert(optimisticUpdates as FolderRecord));

        try {
          await queryFulfilled;
        } catch (err) {
          if (originalFolder) {
            dispatch(folderSlice.actions.upsert(originalFolder));
          }
          toast.error(extractErrorMessage(err, "Failed to update folder. Your changes have been reverted."));
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

export function useDebouncedFolderBatch(folderId: string, delay = 2000) {
  const [mutate] = useBatchUpdateFolderTasksMutation();
  const pendingRef = useRef<Map<string, BatchUpdateFolderTaskValue>>(new Map());
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const mutateRef = useRef(mutate);
  const folderIdRef = useRef(folderId);
  useLayoutEffect(() => { mutateRef.current = mutate; });
  useLayoutEffect(() => { folderIdRef.current = folderId; });

  useEffect(() => () => {
    if (timerRef.current) clearTimeout(timerRef.current);
    const updates = Array.from(pendingRef.current.values());
    pendingRef.current.clear();
    if (updates.length > 0) mutateRef.current({ folderId: folderIdRef.current, updates });
  }, []);

  return useCallback((updates: BatchUpdateFolderTaskValue[]) => {
    updates.forEach(u => {
      const existing = pendingRef.current.get(u.id);
      pendingRef.current.set(u.id, { ...existing, ...u });
    });

    // Optimistic update immediately
    const optimistic = updates
      .filter(u => !u.isDeleted)
      .map(u => ({ ...u, ...(u.clearStartDate ? { startDate: null } : {}), ...(u.clearDueDate ? { dueDate: null } : {}) }));
    const deletedIds = updates.filter(u => u.isDeleted).map(u => u.id);
    if (optimistic.length > 0) store.dispatch(taskSlice.actions.upsertMany(optimistic as Partial<TaskRecord>[]));
    if (deletedIds.length > 0) store.dispatch(taskSlice.actions.removeMany(deletedIds));

    if (timerRef.current) clearTimeout(timerRef.current);
    timerRef.current = setTimeout(() => {
      const toSend = Array.from(pendingRef.current.values());
      pendingRef.current.clear();
      if (toSend.length > 0) mutateRef.current({ folderId: folderIdRef.current, updates: toSend });
    }, delay);
  }, [delay]);
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
        const spaceId = folder?.spaceId;
        if (!spaceId) return [];
        return statuses
          .filter(s => s.spaceId?.toLowerCase() === spaceId.toLowerCase())
          .sort((a, b) => {
            const weightA = statusCategoryWeight[a.category] ?? 4;
            const weightB = statusCategoryWeight[b.category] ?? 4;
            if (weightA !== weightB) return weightA - weightB;
            return ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1);
          });
      }
    ),
  [folder?.spaceId]);

  return useSelector(selectFolderStatuses);
}
