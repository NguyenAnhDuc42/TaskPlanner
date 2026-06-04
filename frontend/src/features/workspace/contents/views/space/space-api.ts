import { workspaceApi } from "@/store/workspaceApi";
import { spaceSlice, folderSlice, taskSlice, statusSlice, entityAccessSlice, folderSelectors, taskSelectors, statusSelectors } from "@/store/entityStore";
import { useSelector } from "react-redux";
import { createSelector } from "@reduxjs/toolkit";
import { useMemo } from "react";
import type { RootState } from "@/store";
import type { SpaceRecord, FolderRecord, TaskRecord } from "@/types/projects";
import type { Status } from "@/types/status";
import { StatusCategory } from "@/types/status-category";
import type { AccessLevel } from "@/types/access-level";
import type { EntityAccessRecord } from "@/types/workspace";
import type { SpaceDocumentRecord } from "@/types/document";


export interface GetSpaceItemsResponse {
  folders: FolderRecord[];
  tasks: TaskRecord[];
  statuses: Status[];
}

export interface EntityAccessRowsValue {
  memberId: string;
  accessLevel: AccessLevel;
  action: "Create" | "Update" | "Delete";
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
      async onQueryStarted(_spaceId, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(spaceSlice.actions.upsert(data));
        } catch {}
      }
    }),

    getSpaceItems: build.query<GetSpaceItemsResponse, string>({
      query: (spaceId) => ({ url: `/spaces/${spaceId}/items`, method: "GET" }),
      async onQueryStarted(_spaceId, { dispatch, queryFulfilled }) {
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
      async onQueryStarted({ spaceId: _spaceId, updates }, { dispatch, queryFulfilled, getState }) {
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
    }),

    getEntityAccess: build.query<EntityAccessRecord[], string>({
      query: (spaceId) => ({ url: `/spaces/${spaceId}/access`, method: "GET" }),
      providesTags: (_result, _error, spaceId) => [{ type: "EntityAccess" as const, id: `access-${spaceId}` }],
      async onQueryStarted(_spaceId, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          const mapped = data.map(item => ({ ...item, id: item.workspaceMemberId }));
          dispatch(entityAccessSlice.actions.upsertMany(mapped));
        } catch {}
      }
    }),

    updateEntityAccess: build.mutation<void, { spaceId: string; rows: EntityAccessRowsValue[] }>({
      query: ({ spaceId, rows }) => ({
        url: `/spaces/${spaceId}/access`,
        method: "POST",
        data: rows
      }),
      invalidatesTags: (_result, _error, { spaceId }) => [{ type: "EntityAccess" as const, id: `access-${spaceId}` }],
      async onQueryStarted({ spaceId: _spaceId, rows }, { dispatch, queryFulfilled, getState }) {
        const state = getState() as RootState;

        // Snapshot original rows for rollback
        const originalStateRows: EntityAccessRecord[] = [];
        const idsToRemoveOnFailure: string[] = [];

        rows.forEach((r) => {
          const original = state.entityAccess.entities[r.memberId];
          if (original) {
            originalStateRows.push(original);
          } else {
            idsToRemoveOnFailure.push(r.memberId);
          }
        });

        const newRows = rows.map(r => ({
          id: r.memberId,
          workspaceMemberId: r.memberId,
          accessLevel: r.accessLevel,
          haveAccess: r.action !== "Delete"
        }));

        dispatch(entityAccessSlice.actions.upsertMany(newRows));

        try {
          await queryFulfilled;
        } catch {
          if (idsToRemoveOnFailure.length > 0) {
            dispatch(entityAccessSlice.actions.removeMany(idsToRemoveOnFailure));
          }
        }
      }
    }),

    getSpaceDocuments: build.query<SpaceDocumentRecord[], string>({
      query: (spaceId) => ({ url: `/spaces/${spaceId}/documents`, method: "GET" }),
      providesTags: (_result, _error, spaceId) => [{ type: "Spaces" as const, id: `docs-${spaceId}` }]
    }),

    createSpaceDocument: build.mutation<SpaceDocumentRecord, { spaceId: string; name: string }>({
      query: ({ spaceId, name }) => ({
        url: `/spaces/${spaceId}/documents`,
        method: "POST",
        data: { name }
      }),
      invalidatesTags: (_result, _error, { spaceId }) => [{ type: "Spaces" as const, id: `docs-${spaceId}` }]
    })
  })
});


export const {
  useGetSpaceDetailQuery,
  useGetSpaceItemsQuery,
  useBatchUpdateSpaceItemsMutation,
  useUpdateSpaceFieldMutation,
  useGetEntityAccessQuery,
  useUpdateEntityAccessMutation,
  useGetSpaceDocumentsQuery,
  useCreateSpaceDocumentMutation,
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
          .filter((t) => t.spaceId?.toLowerCase() === targetSpaceId && !t.folderId)
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
          .filter(s => s.workflowId?.toLowerCase() === targetWorkflowId.toLowerCase())
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
