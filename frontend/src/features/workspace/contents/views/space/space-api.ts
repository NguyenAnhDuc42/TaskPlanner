import { workspaceApi } from "@/store/workspaceApi";
import { spaceSlice, folderSlice, taskSlice, statusSlice, entityAccessSlice, folderSelectors, taskSelectors, statusSelectors } from "@/store/entityStore";
import { RowAction } from "@/types/row-action";
import type { AccessLevel } from "@/types/access-level";
import { useSelector, useDispatch } from "react-redux";
import { createSelector } from "@reduxjs/toolkit";
import { useMemo, useEffect } from "react";
import type { RootState, AppDispatch } from "@/store";
import type { SpaceRecord, FolderRecord, TaskRecord } from "@/types/projects";
import type { Status } from "@/types/status";
import { StatusCategory } from "@/types/status-category";
import type { EntityAccessRecord } from "@/types/workspace";

import { EntityLayerType } from "@/types/entity-layer-type";


export interface GetSpaceItemsResponse {
  folders: FolderRecord[];
  tasks: TaskRecord[];
  statuses: Status[];
  hasNextPage: boolean;
  nextCursor: string | null;
}

export interface EntityAccessRowsValue {
  id?: string;
  memberId: string;
  accessLevel: AccessLevel;
  action: RowAction;
}

export interface SpaceBoardFilter {
  priorities?: string[];
  folderIds?: string[]; // "__none__" = tasks with no folder (direct space tasks)
  search?: string;
  startDate?: string;
  dueDate?: string;
}

export interface BatchUpdateSpaceItemValue {
  id: string;
  type: EntityLayerType;
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
      async onQueryStarted(_, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(spaceSlice.actions.upsert(data));
        } catch (error) {
          console.error("[spaceApi] Failed to sync space detail to store:", error);
        }
      }
    }),

    getSpaceItems: build.query<GetSpaceItemsResponse, { spaceId: string; cursor?: string | null }>({
      query: ({ spaceId, cursor }) => ({
        url: `/spaces/${spaceId}/items`,
        method: "GET",
        params: cursor ? { cursor } : undefined,
      }),
      async onQueryStarted(_, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(folderSlice.actions.upsertMany(data.folders));
          dispatch(taskSlice.actions.upsertMany(data.tasks));
          dispatch(statusSlice.actions.upsertMany(data.statuses));
        } catch (error) {
          console.error("[spaceApi] Failed to sync space items to store:", error);
        }
      }
    }),


    batchUpdateSpaceItems: build.mutation<void, { spaceId: string; updates: BatchUpdateSpaceItemValue[] }>({
      query: ({ spaceId, updates }) => ({
        url: `/spaces/${spaceId}/batch-update`,
        method: "PUT",
        data: { updates }
      }),
      // No onQueryStarted — useDebouncedSpaceBatch owns the optimistic update and rollback.
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
    }),

    getEntityAccess: build.query<EntityAccessRecord[], string>({
      query: (spaceId) => ({ url: `/spaces/${spaceId}/access`, method: "GET" }),
      providesTags: (_result, _error, spaceId) => [{ type: "EntityAccess" as const, id: `access-${spaceId}` }],
      async onQueryStarted(_, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          // Filter out rows without database Guid ids (accessLevel = None / HaveAccess = false)
          const mapped = data
            .filter(item => item.id)
            .map(item => ({ ...item, id:item.id }));
          dispatch(entityAccessSlice.actions.upsertMany(mapped));
        } catch (error) {
          console.error("[spaceApi] Failed to sync entity access to store:", error);
        }
      }
    }),

    updateEntityAccess: build.mutation<void, { spaceId: string; rows: EntityAccessRowsValue[] }>({
      query: ({ spaceId, rows }) => ({
        url: `/spaces/${spaceId}/access`,
        method: "POST",
        data: rows
      }),
      async onQueryStarted({ spaceId, rows }, { dispatch, queryFulfilled }) {
        // Optimistic only for delete and level changes — NOT for create (avoids temp ID duplicates)
        const deletedIds: string[] = [];

        for (const row of rows) {
          if (row.action === RowAction.Delete && row.id) {
            deletedIds.push(row.id);
            dispatch(entityAccessSlice.actions.remove(row.id));
          } else if (row.action === RowAction.Update && row.id) {
            dispatch(entityAccessSlice.actions.upsert({
              id: row.id, spaceId,
              workspaceMemberId: row.memberId,
              accessLevel: row.accessLevel as AccessLevel,
              haveAccess: true,
            }));
          }
        }

        try {
          await queryFulfilled;
        } catch {
          // Refetch will restore correct state on failure
        }
      },
      invalidatesTags: (_result, _error, { spaceId }) => [{ type: "EntityAccess" as const, id: `access-${spaceId}` }],
    }),

  })
});


export const {
  useGetSpaceDetailQuery,
  useGetSpaceItemsQuery,
  useBatchUpdateSpaceItemsMutation,
  useUpdateSpaceFieldMutation,
  useGetEntityAccessQuery,
  useUpdateEntityAccessMutation,
} = spaceApi;

// Eagerly loads all space tasks in 200-item batches, chaining until hasNextPage = false.
// Each batch upserts into entity store via onQueryStarted — no view state needed.
export function useSpaceItemsFullLoad(spaceId: string) {
  const dispatch = useDispatch<AppDispatch>();
  const { data, isSuccess } = useGetSpaceItemsQuery({ spaceId, cursor: null }, { skip: !spaceId });

  useEffect(() => {
    if (!spaceId || !isSuccess || !data?.hasNextPage || !data?.nextCursor) return;

    async function fetchRemaining(cursor: string) {
      const result = await dispatch(
        spaceApi.endpoints.getSpaceItems.initiate({ spaceId, cursor }, { subscribe: false })
      );
      if (result.data?.hasNextPage && result.data?.nextCursor) {
        await fetchRemaining(result.data.nextCursor);
      }
    }

    fetchRemaining(data.nextCursor);
  }, [isSuccess, data?.hasNextPage, data?.nextCursor, spaceId, dispatch]);

  return { isLoading: !isSuccess, isFullyLoaded: isSuccess && !data?.hasNextPage };
}

// Selectors
export function useSpaceDetail(spaceId: string) {
  return useSelector((state: RootState) => state.spaces.entities[spaceId]);
}

export type BoardItem = TaskRecord & { __type: "task"; folderName?: string };

export function useSpaceBoardItems(spaceId: string) {
  const selectBoardItemsForSpace = useMemo(() =>
    createSelector(
      [taskSelectors.selectAll, folderSelectors.selectEntities],
      (tasks, folderMap) => {
        const targetSpaceId = spaceId.toLowerCase();
        return tasks
          .filter((t) => t.spaceId?.toLowerCase() === targetSpaceId && !t.parentTaskId)
          .map((t) => ({
            ...t,
            __type: "task" as const,
            folderName: t.folderId ? folderMap[t.folderId]?.name : undefined,
          })) as BoardItem[];
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
        const spaceId = space?.id;
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
  [space?.id]);

  return useSelector(selectSpaceStatuses);
}
