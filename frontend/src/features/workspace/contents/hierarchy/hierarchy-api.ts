import { workspaceApi } from "@/store/workspaceApi";
import { spaceSlice, folderSlice, taskSlice, spaceSelectors, folderSelectors, taskSelectors } from "@/store/entityStore";
import { useSelector } from "react-redux";
import type { RootState } from "@/store";
import type { SpaceRecord, FolderRecord, TaskRecord } from "@/types/projects";
import type { PagedResult } from "@/types/paged-result";
import { EntityLayerType } from "@/types/entity-layer-type";
import { createSelector } from "@reduxjs/toolkit";
import { useMemo } from "react";
import { toast } from "sonner";
import { extractErrorMessage } from "@/types/api-error";

export interface CreateSpaceRequest {
  name: string;
  isPrivate: boolean;
  color?: string;
  icon?: string;
  memberIdsToInvite?: string[];
}

export interface CreateFolderRequest {
  spaceId: string;
  name: string;
  color?: string;
  icon?: string;
  startDate?: string;
  dueDate?: string;
}

export interface CreateTaskRequest {
  name: string;
  parentId: string;
  parentType: string;
  icon?: string;
  color?: string;
  statusId?: string | null;
  priority?: string | null;
  assignees?: string[];
  startDate?: string;
  dueDate?: string;
}

export interface MoveItemRequest {
  itemId: string;
  itemType: string;
  targetParentId: string;
  targetParentType: string;
  nextItemOrderKey?: string;
  newOrderKey?: string;
  sourceParentId?: string;
  sourceParentType?: string;
}

export interface SpaceMoveValue { itemId: string; newOrderKey: string; }
export interface FolderMoveValue { itemId: string; targetParentId: string | null; newOrderKey: string; }
export interface TaskMoveValue { itemId: string; targetSpaceId: string; targetFolderId: string | null; newOrderKey: string; }
export interface BatchMoveCommand {
  spaces?: SpaceMoveValue[];
  folders?: FolderMoveValue[];
  tasks?: TaskMoveValue[];
}

export const hierarchyApi = workspaceApi.injectEndpoints({
  endpoints: (build) => ({
    // Load spaces paginated
    getNodeSpaces: build.query<PagedResult<SpaceRecord>, { workspaceId: string; cursor: string | null }>({
      query: ({ workspaceId, cursor }) => ({
        url: `/workspaces/${workspaceId}/nodes/spaces`,
        method: "GET",
        params: { cursor, pageSize: 10 }
      }),
      serializeQueryArgs: ({ endpointName, queryArgs }) => `${endpointName}_${queryArgs.workspaceId}`,
      merge: (currentCache, newItems) => {
        if (!currentCache) return newItems;
        currentCache.items = [...currentCache.items, ...newItems.items];
        currentCache.nextCursor = newItems.nextCursor;
        currentCache.hasNextPage = newItems.hasNextPage;
        return currentCache;
      },
      forceRefetch({ currentArg, previousArg }) {
        return currentArg?.cursor !== previousArg?.cursor;
      },
      async onQueryStarted(_, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(spaceSlice.actions.upsertMany(data.items));
        } catch (error) {
          console.error("[hierarchyApi] Failed to sync spaces to store:", error);
        }
      }
    }),

    // Load folders for a space paginated
    getNodeFolders: build.query<PagedResult<FolderRecord>, { workspaceId: string; nodeId: string; cursor: string | null }>({
      query: ({ workspaceId, nodeId, cursor }) => ({
        url: `/workspaces/${workspaceId}/nodes/${nodeId}/folders`,
        method: "GET",
        params: { cursor, pageSize: 10 }
      }),
      serializeQueryArgs: ({ endpointName, queryArgs }) => `${endpointName}_${queryArgs.nodeId}`,
      merge: (currentCache, newItems) => {
        if (!currentCache) return newItems;
        currentCache.items = [...currentCache.items, ...newItems.items];
        currentCache.nextCursor = newItems.nextCursor;
        currentCache.hasNextPage = newItems.hasNextPage;
        return currentCache;
      },
      forceRefetch({ currentArg, previousArg }) {
        return currentArg?.cursor !== previousArg?.cursor;
      },
      async onQueryStarted({ nodeId, cursor }, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(folderSlice.actions.upsertMany(data.items));
          if (cursor === null && data.items.length === 0) {
            dispatch(spaceSlice.actions.upsert({ id: nodeId, hasFolders: false }));
          }
        } catch (error) {
          console.error("[hierarchyApi] Failed to sync folders to store:", error);
        }
      }
    }),

    // Load tasks for a folder/space paginated
    getNodeTasks: build.query<PagedResult<TaskRecord>, { workspaceId: string; nodeId: string; parentType: EntityLayerType; cursor: string | null }>({
      query: ({ workspaceId, nodeId, parentType, cursor }) => ({
        url: `/workspaces/${workspaceId}/nodes/${nodeId}/tasks`,
        method: "GET",
        params: { parentType, cursor, pageSize: 10 }
      }),
      serializeQueryArgs: ({ endpointName, queryArgs }) => `${endpointName}_${queryArgs.nodeId}`,
      merge: (currentCache, newItems) => {
        if (!currentCache) return newItems;
        currentCache.items = [...currentCache.items, ...newItems.items];
        currentCache.nextCursor = newItems.nextCursor;
        currentCache.hasNextPage = newItems.hasNextPage;
        return currentCache;
      },
      forceRefetch({ currentArg, previousArg }) {
        return currentArg?.cursor !== previousArg?.cursor;
      },
      async onQueryStarted({ nodeId, parentType, cursor }, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(taskSlice.actions.upsertMany(data.items));
          if (cursor === null && data.items.length === 0) {
            if (parentType === EntityLayerType.ProjectSpace) {
              dispatch(spaceSlice.actions.upsert({ id: nodeId, hasTasks: false }));
            } else if (parentType === EntityLayerType.ProjectFolder) {
              dispatch(folderSlice.actions.upsert({ id: nodeId, hasTasks: false }));
            }
          }
        } catch (error) {
          console.error("[hierarchyApi] Failed to sync tasks to store:", error);
        }
      }
    }),

    createSpace: build.mutation<SpaceRecord, { workspaceId: string; body: CreateSpaceRequest }>({
      query: ({ body }) => ({ url: `/spaces`, method: "POST", data: body }),
      async onQueryStarted(_, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(spaceSlice.actions.upsert(data));
        } catch (error) {
          console.error("Create space API call failed:", error);
        }
      }
    }),

    createFolder: build.mutation<FolderRecord, { workspaceId: string; body: CreateFolderRequest }>({
      query: ({ body }) => ({ url: `/folders`, method: "POST", data: body }),
      async onQueryStarted({ body }, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(folderSlice.actions.upsert(data));
          dispatch(spaceSlice.actions.upsert({ id: body.spaceId, hasFolders: true }));
        } catch (error) {
          console.error("Create folder API call failed:", error);
        }
      }
    }),

    createTask: build.mutation<TaskRecord, { workspaceId: string; body: CreateTaskRequest }>({
      query: ({ body }) => ({ url: `/tasks`, method: "POST", data: body }),
      async onQueryStarted({ body }, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(taskSlice.actions.upsert(data));
          
          if (body.parentType === EntityLayerType.ProjectSpace) {
            dispatch(spaceSlice.actions.upsert({ id: body.parentId, hasTasks: true }));
          } else if (body.parentType === EntityLayerType.ProjectFolder) {
            dispatch(folderSlice.actions.upsert({ id: body.parentId, hasTasks: true }));
          }
        } catch (error) {
          console.error("Create task API call failed:", error);
        }
      }
    }),

    deleteSpace: build.mutation<void, { workspaceId: string; spaceId: string }>({
      query: ({ spaceId }) => ({ url: `/spaces/${spaceId}`, method: "DELETE" }),
      async onQueryStarted({ spaceId }, { dispatch, queryFulfilled, getState }) {
        const state = getState() as RootState;
        const originalSpace = spaceSelectors.selectById(state, spaceId);
        dispatch(spaceSlice.actions.remove(spaceId));
        try { 
          await queryFulfilled; 
        } catch (err) {
          if (originalSpace) dispatch(spaceSlice.actions.upsert(originalSpace));
          toast.error(extractErrorMessage(err, "Failed to delete space."));
        }
      }
    }),

    deleteFolder: build.mutation<void, { workspaceId: string; folderId: string }>({
      query: ({ folderId }) => ({ url: `/folders/${folderId}`, method: "DELETE" }),
      async onQueryStarted({ folderId }, { dispatch, queryFulfilled, getState }) {
        const state = getState() as RootState;
        const originalFolder = folderSelectors.selectById(state, folderId);
        dispatch(folderSlice.actions.remove(folderId));
        try { 
          await queryFulfilled; 
        } catch (err) {
          if (originalFolder) dispatch(folderSlice.actions.upsert(originalFolder));
          toast.error(extractErrorMessage(err, "Failed to delete folder."));
        }
      }
    }),

    deleteTask: build.mutation<void, { workspaceId: string; taskId: string }>({
      query: ({ taskId }) => ({ url: `/tasks/${taskId}`, method: "DELETE" }),
      async onQueryStarted({ taskId }, { dispatch, queryFulfilled, getState }) {
        const state = getState() as RootState;
        const originalTask = taskSelectors.selectById(state, taskId);
        dispatch(taskSlice.actions.remove(taskId));
        try { 
          await queryFulfilled; 
        } catch (err) {
          if (originalTask) dispatch(taskSlice.actions.upsert(originalTask));
          toast.error(extractErrorMessage(err, "Failed to delete task."));
        }
      }
    }),

    

    batchMoveItems: build.mutation<void, { workspaceId: string; command: BatchMoveCommand }>({
      query: ({ workspaceId, command }) => ({
        url: `/workspaces/${workspaceId}/nodes/batch-move`,
        method: "POST",
        data: command
      }),
      async onQueryStarted({ workspaceId, command }, { dispatch, queryFulfilled, getState }) {
        const state = getState() as RootState;
        const patches: { undo: () => void }[] = [];

        command.spaces?.forEach((move) => {
          const space = spaceSelectors.selectById(state, move.itemId);
          if (space) {
            const patch = dispatch(
              hierarchyApi.util.updateQueryData("getNodeSpaces", { workspaceId, cursor: null }, (draft) => {
                if (!draft?.items) return;
                const idx = draft.items.findIndex(s => s.id === move.itemId);
                if (idx === -1) {
                  draft.items.push({ ...space, orderKey: move.newOrderKey });
                } else {
                  draft.items[idx].orderKey = move.newOrderKey;
                }
                draft.items.sort((a, b) => ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1));
              })
            );
            patches.push(patch);
          }
        });

        command.folders?.forEach((move) => {
          const folder = folderSelectors.selectById(state, move.itemId);
          if (folder) {
            const targetSpaceId = move.targetParentId ?? folder.spaceId;
            if (!targetSpaceId) return;

            const patch = dispatch(
              hierarchyApi.util.updateQueryData("getNodeFolders", { workspaceId, nodeId: targetSpaceId, cursor: null }, (draft) => {
                if (!draft?.items) return;
                const idx = draft.items.findIndex(f => f.id === move.itemId);
                if (idx === -1) {
                  draft.items.push({ ...folder, orderKey: move.newOrderKey, spaceId: targetSpaceId });
                } else {
                  draft.items[idx].orderKey = move.newOrderKey;
                  draft.items[idx].spaceId = targetSpaceId;
                }
                draft.items.sort((a, b) => ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1));
              })
            );
            patches.push(patch);

            if (move.targetParentId && folder.spaceId && move.targetParentId !== folder.spaceId) {
              const removePatch = dispatch(
                hierarchyApi.util.updateQueryData("getNodeFolders", { workspaceId, nodeId: folder.spaceId, cursor: null }, (draft) => {
                  if (!draft?.items) return;
                  const idx = draft.items.findIndex(f => f.id === move.itemId);
                  if (idx !== -1) {
                    draft.items.splice(idx, 1);
                  }
                })
              );
              patches.push(removePatch);
            }
          }
        });

        command.tasks?.forEach((move) => {
          const task = taskSelectors.selectById(state, move.itemId);
          if (task) {
            const containerNodeId = move.targetFolderId ?? move.targetSpaceId;
            const parentType = (move.targetFolderId ? "ProjectFolder" : "ProjectSpace") as EntityLayerType;
            const patch = dispatch(
              hierarchyApi.util.updateQueryData(
                "getNodeTasks",
                { workspaceId, nodeId: containerNodeId, parentType, cursor: null },
                (draft) => {
                  if (!draft?.items) return;
                  const idx = draft.items.findIndex(t => t.id === move.itemId);
                  if (idx === -1) {
                    draft.items.push({
                      ...task,
                      orderKey: move.newOrderKey,
                      spaceId: move.targetSpaceId,
                      folderId: move.targetFolderId
                    });
                  } else {
                    draft.items[idx].orderKey = move.newOrderKey;
                  }
                  draft.items.sort((a, b) => ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1));
                }
              )
            );
            patches.push(patch);

            const oldContainerId = task.folderId ?? task.spaceId;
            const oldParentType = (task.folderId ? "ProjectFolder" : "ProjectSpace") as EntityLayerType;
            if (oldContainerId && (oldContainerId !== containerNodeId || oldParentType !== parentType)) {
              const removePatch = dispatch(
                hierarchyApi.util.updateQueryData(
                  "getNodeTasks",
                  { workspaceId, nodeId: oldContainerId, parentType: oldParentType, cursor: null },
                  (draft) => {
                    if (!draft?.items) return;
                    const idx = draft.items.findIndex(t => t.id === move.itemId);
                    if (idx !== -1) {
                      draft.items.splice(idx, 1);
                    }
                  }
                )
              );
              patches.push(removePatch);
            }
          }
        });

        try {
          await queryFulfilled;
        } catch {
          patches.forEach((p) => p.undo());
        }
      }
    })
  })
});

export const {
  useGetNodeSpacesQuery,
  useGetNodeFoldersQuery,
  useGetNodeTasksQuery,
  useCreateSpaceMutation,
  useCreateFolderMutation,
  useCreateTaskMutation,
  useDeleteSpaceMutation,
  useDeleteFolderMutation,
  useDeleteTaskMutation,
  useBatchMoveItemsMutation,
} = hierarchyApi;



// --- CENTRAL RELATIONAL SELECTORS FOR COMPONENTS ---
export function useSpaces(workspaceId: string) {
  const selectForWorkspace = useMemo(() =>
    createSelector(
      [spaceSelectors.selectAll],
      (spaces) => spaces
        .filter(s => s.workspaceId === workspaceId)
        .sort((a, b) => ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1))
    ),
  [workspaceId]);

  return useSelector(selectForWorkspace);
}

export function useFoldersBySpace(spaceId: string) {
  const selectForThisSpace = useMemo(() => {
    return createSelector(
      [folderSelectors.selectAll],
      (folders) => folders
        .filter(f => f.spaceId === spaceId)
        .sort((a, b) => ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1))
    );
  }, [spaceId]);

  return useSelector(selectForThisSpace);
}

export function useTasksByParent(parentId: string) {
  const selectForThisParent = useMemo(() => {
    return createSelector(
      [taskSelectors.selectAll],
      (tasks) => tasks
        .filter(t => 
          !t.parentTaskId && (
            t.folderId === parentId || 
            (t.spaceId === parentId && !t.folderId)
          )
        )
        .sort((a, b) => ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1))
    );
  }, [parentId]);

  return useSelector(selectForThisParent);
}
