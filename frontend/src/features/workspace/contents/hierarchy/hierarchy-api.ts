import { workspaceApi } from "@/store/workspaceApi";
import { spaceSlice, folderSlice, taskSlice, spaceSelectors, folderSelectors, taskSelectors } from "@/store/entityStore";
import { useSelector } from "react-redux";
import type { RootState } from "@/store";
import type { SpaceRecord, FolderRecord, TaskRecord } from "@/types/projects";
import type { PagedResult } from "@/types/paged-result";
import type { EntityLayerType } from "@/types/entity-layer-type";
import { createSelector } from "@reduxjs/toolkit";

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
  statusId?: string | null;
  startDate?: string;
  dueDate?: string;
  priority?: Priority;
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

export const hierarchyApi = workspaceApi.injectEndpoints({
  endpoints: (build) => ({
    // Load spaces paginated
    getNodeSpaces: build.query<PagedResult<SpaceRecord>, { workspaceId: string; cursor: string | null }>({
      query: ({ workspaceId, cursor }) => ({
        url: `/workspaces/${workspaceId}/nodes/spaces`,
        method: "GET",
        params: { cursor }
      }),
      serializeQueryArgs: ({ endpointName, queryArgs }) => `${endpointName}_${queryArgs.workspaceId}`,
      merge: (currentCache, newItems) => {
        if (!currentCache) return newItems;
        currentCache.items = [...currentCache.items, ...newItems.items];
        currentCache.nextCursor = newItems.nextCursor;
        currentCache.hasNextPage = newItems.hasNextPage;
        return currentCache;
      },
      async onQueryStarted(_, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(spaceSlice.actions.upsertMany(data.items));
        } catch {}
      }
    }),

    // Load folders for a space paginated
    getNodeFolders: build.query<PagedResult<FolderRecord>, { workspaceId: string; nodeId: string; cursor: string | null }>({
      query: ({ workspaceId, nodeId, cursor }) => ({
        url: `/workspaces/${workspaceId}/nodes/${nodeId}/folders`,
        method: "GET",
        params: { cursor }
      }),
      serializeQueryArgs: ({ endpointName, queryArgs }) => `${endpointName}_${queryArgs.nodeId}`,
      merge: (currentCache, newItems) => {
        if (!currentCache) return newItems;
        currentCache.items = [...currentCache.items, ...newItems.items];
        currentCache.nextCursor = newItems.nextCursor;
        currentCache.hasNextPage = newItems.hasNextPage;
        return currentCache;
      },
      async onQueryStarted(_, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(folderSlice.actions.upsertMany(data.items));
        } catch {}
      }
    }),

    // Load tasks for a folder/space paginated
    getNodeTasks: build.query<PagedResult<TaskRecord>, { workspaceId: string; nodeId: string; parentType: EntityLayerType; cursor: string | null }>({
      query: ({ workspaceId, nodeId, parentType, cursor }) => ({
        url: `/workspaces/${workspaceId}/nodes/${nodeId}/tasks`,
        method: "GET",
        params: { parentType, cursor }
      }),
      serializeQueryArgs: ({ endpointName, queryArgs }) => `${endpointName}_${queryArgs.nodeId}`,
      merge: (currentCache, newItems) => {
        if (!currentCache) return newItems;
        currentCache.items = [...currentCache.items, ...newItems.items];
        currentCache.nextCursor = newItems.nextCursor;
        currentCache.hasNextPage = newItems.hasNextPage;
        return currentCache;
      },
      async onQueryStarted(_, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(taskSlice.actions.upsertMany(data.items));
        } catch {}
      }
    }),

    createSpace: build.mutation<SpaceRecord, { workspaceId: string; body: CreateSpaceRequest }>({
      query: ({ body }) => ({ url: `/spaces`, method: "POST", data: body }),
      async onQueryStarted({ workspaceId, body }, { dispatch, queryFulfilled }) {
        // optimistic — fake id until server responds
        const tempId = `temp_${crypto.randomUUID()}`
        const optimistic: SpaceRecord = {
          id: tempId,
          workspaceId,
          name: body.name,
          isPrivate: body.isPrivate,
          color: body.color ?? null,
          icon: body.icon ?? null,
        } as SpaceRecord

        dispatch(spaceSlice.actions.upsert(optimistic))

        try {
          const { data } = await queryFulfilled
          // remove temp, add real
          dispatch(spaceSlice.actions.remove(tempId))
          
          const spaceId = typeof data === 'string' ? data : (data as any).id || (data as any).value || tempId;
          const realSpace: SpaceRecord = {
            id: spaceId,
            workspaceId,
            name: body.name,
            isPrivate: body.isPrivate,
            color: body.color ?? null,
            icon: body.icon ?? null,
          } as SpaceRecord;

          dispatch(spaceSlice.actions.upsert(realSpace))
        } catch {
          // server failed, remove temp
          dispatch(spaceSlice.actions.remove(tempId))
        }
      }
    }),

    createFolder: build.mutation<FolderRecord, { workspaceId: string; body: CreateFolderRequest }>({
      query: ({ body }) => ({ url: `/folders`, method: "POST", data: body }),
      async onQueryStarted({ body }, { dispatch, queryFulfilled }) {
        const tempId = `temp_${crypto.randomUUID()}`
        const optimistic: FolderRecord = {
          id: tempId,
          name: body.name,
          spaceId: body.spaceId,
          color: body.color ?? null,
          icon: body.icon ?? null,
          statusId: body.statusId ?? null,
          priority: body.priority ?? null,
          startDate: body.startDate ?? null,
          dueDate: body.dueDate ?? null,
        } as FolderRecord

        dispatch(folderSlice.actions.upsert(optimistic))

        try {
          const { data } = await queryFulfilled
          dispatch(folderSlice.actions.remove(tempId))
          
          const folderId = typeof data === 'string' ? data : (data as any).id || (data as any).value || tempId;
          const realFolder: FolderRecord = {
            id: folderId,
            spaceId: body.spaceId,
            name: body.name,
            color: body.color ?? null,
            icon: body.icon ?? null,
            statusId: body.statusId ?? null,
            priority: body.priority ?? null,
            startDate: body.startDate ?? null,
            dueDate: body.dueDate ?? null,
          } as FolderRecord;

          dispatch(folderSlice.actions.upsert(realFolder))
        } catch {
          dispatch(folderSlice.actions.remove(tempId))
        }
      }
    }),

    createTask: build.mutation<TaskRecord, { workspaceId: string; body: CreateTaskRequest }>({
      query: ({ body }) => ({ url: `/tasks`, method: "POST", data: body }),
      async onQueryStarted({ body }, { dispatch, queryFulfilled }) {
        const tempId = `temp_${crypto.randomUUID()}`
        const optimistic: TaskRecord = {
          id: tempId,
          name: body.name,
          projectFolderId: body.parentType === "ProjectFolder" ? body.parentId : null,
          projectSpaceId:  body.parentType === "ProjectSpace"  ? body.parentId : null,
          icon: body.icon ?? null,
          color: body.color ?? null,
          statusId: body.statusId ?? null,
          priority: body.priority ?? null,
          assigneeIds: body.assignees ?? [],
          startDate: body.startDate ?? null,
          dueDate: body.dueDate ?? null,
        } as TaskRecord

        dispatch(taskSlice.actions.upsert(optimistic))

        try {
          const { data } = await queryFulfilled
          dispatch(taskSlice.actions.remove(tempId))
          
          const taskId = typeof data === 'string' ? data : (data as any).id || (data as any).value || tempId;
          const realTask: TaskRecord = {
            id: taskId,
            name: body.name,
            projectFolderId: body.parentType === "ProjectFolder" ? body.parentId : null,
            projectSpaceId:  body.parentType === "ProjectSpace"  ? body.parentId : null,
            icon: body.icon ?? null,
            color: body.color ?? null,
            statusId: body.statusId ?? null,
            priority: body.priority ?? null,
            assigneeIds: body.assignees ?? [],
            startDate: body.startDate ?? null,
            dueDate: body.dueDate ?? null,
          } as TaskRecord;

          dispatch(taskSlice.actions.upsert(realTask))
        } catch {
          dispatch(taskSlice.actions.remove(tempId))
        }
      }
    }),

    deleteSpace: build.mutation<void, { workspaceId: string; spaceId: string }>({
      query: ({ spaceId }) => ({ url: `/spaces/${spaceId}`, method: "DELETE" }),
      async onQueryStarted({ spaceId }, { dispatch, queryFulfilled }) {
        dispatch(spaceSlice.actions.remove(spaceId));
        try { await queryFulfilled; } catch {}
      }
    }),

    deleteFolder: build.mutation<void, { workspaceId: string; folderId: string }>({
      query: ({ folderId }) => ({ url: `/folders/${folderId}`, method: "DELETE" }),
      async onQueryStarted({ folderId }, { dispatch, queryFulfilled }) {
        dispatch(folderSlice.actions.remove(folderId));
        try { await queryFulfilled; } catch {}
      }
    }),

    deleteTask: build.mutation<void, { workspaceId: string; taskId: string }>({
      query: ({ taskId }) => ({ url: `/tasks/${taskId}`, method: "DELETE" }),
      async onQueryStarted({ taskId }, { dispatch, queryFulfilled }) {
        dispatch(taskSlice.actions.remove(taskId));
        try { await queryFulfilled; } catch {}
      }
    }),

    moveItem: build.mutation<void, { workspaceId: string; body: MoveItemRequest }>({
      query: ({ workspaceId, body }) => ({ url: `/workspaces/${workspaceId}/nodes/move`, method: "POST", data: body }),
      async onQueryStarted({ workspaceId, body }, { dispatch, queryFulfilled, getState }) {
        const state = getState() as RootState;
        const itemId = body.itemId;

        // 1. Instantly update the item's flat properties inside your entity store
        if (body.itemType === "ProjectSpace") {
          const space = spaceSelectors.selectById(state, itemId);
          if (space && body.newOrderKey) {
            dispatch(spaceSlice.actions.upsert({ ...space, orderKey: body.newOrderKey }));
          }
        } 
        else if (body.itemType === "ProjectFolder") {
          const folder = folderSelectors.selectById(state, itemId);
          if (folder && body.newOrderKey) {
            dispatch(folderSlice.actions.upsert({ ...folder, orderKey: body.newOrderKey, spaceId: body.targetParentId }));
            
            // 🔥 OPTIMISTIC FLAG UPDATE: Turn on hasFolders for the new target Space
            const targetSpace = spaceSelectors.selectById(state, body.targetParentId);
            if (targetSpace) {
              dispatch(spaceSlice.actions.upsert({ ...targetSpace, hasFolders: true }));
            }
          }
        } 
        else if (body.itemType === "ProjectTask") {
          const task = taskSelectors.selectById(state, itemId);
          if (task && body.newOrderKey) {
            const isTargetSpace = body.targetParentType === "ProjectSpace";
            dispatch(taskSlice.actions.upsert({ 
              ...task, 
              orderKey: body.newOrderKey, 
              projectSpaceId: isTargetSpace ? body.targetParentId : task.projectSpaceId,
              projectFolderId: isTargetSpace ? undefined : body.targetParentId
            }));

            // 🔥 OPTIMISTIC FLAG UPDATE: Turn on hasTasks for the new target parent
            if (isTargetSpace) {
              const targetSpace = spaceSelectors.selectById(state, body.targetParentId);
              if (targetSpace) {
                dispatch(spaceSlice.actions.upsert({ ...targetSpace, hasTasks: true }));
              }
            } else {
              const targetFolder = folderSelectors.selectById(state, body.targetParentId);
              if (targetFolder) {
                dispatch(folderSlice.actions.upsert({ ...targetFolder, hasTasks: true }));
              }
            }
          }
        }

        // 2. SURGICALLY UPDATE THE LAZY-LOADED QUERY CACHES
        if (body.itemType === "ProjectFolder") {
          dispatch(
            hierarchyApi.util.updateQueryData("getNodeFolders", { workspaceId, nodeId: body.targetParentId, cursor: null }, (draft) => {
              if (!draft || !draft.items) return;
              
              const itemIndex = draft.items.findIndex(f => f.id === itemId);
              if (itemIndex !== -1) {
                draft.items[itemIndex].orderKey = body.newOrderKey ?? draft.items[itemIndex].orderKey;
              } else {
                const folder = folderSelectors.selectById(state, itemId);
                if (folder) {
                  draft.items.push({ ...folder, orderKey: body.newOrderKey ?? "", spaceId: body.targetParentId });
                }
              }
              draft.items.sort((a, b) => (a.orderKey ?? "").localeCompare(b.orderKey ?? ""));
            })
          );
        }

        else if (body.itemType === "ProjectTask") {
          dispatch(
            hierarchyApi.util.updateQueryData(
              "getNodeTasks", 
              { workspaceId, nodeId: body.targetParentId, parentType: body.targetParentType as EntityLayerType, cursor: null }, 
              (draft) => {
                if (!draft || !draft.items) return;

                const itemIndex = draft.items.findIndex(t => t.id === itemId);
                if (itemIndex !== -1) {
                  draft.items[itemIndex].orderKey = body.newOrderKey ?? draft.items[itemIndex].orderKey;
                } else {
                  const task = taskSelectors.selectById(state, itemId);
                  if (task) {
                    const isTargetSpace = body.targetParentType === "ProjectSpace";
                    draft.items.push({
                      ...task,
                      orderKey: body.newOrderKey ?? "",
                      projectSpaceId: isTargetSpace ? body.targetParentId : task.projectSpaceId,
                      projectFolderId: isTargetSpace ? undefined : body.targetParentId
                    });
                  }
                }
                draft.items.sort((a, b) => (a.orderKey ?? "").localeCompare(b.orderKey ?? ""));
              }
            )
          );
        }

        try {
          await queryFulfilled;
        } catch {
          // If server fails, your fallback/refresh runs
        }
      }
    }),

    batchMoveItems: build.mutation<void, { workspaceId: string; moves: { itemId: string; itemType: string; targetParentId: string | null; newOrderKey: string }[] }>({
      query: ({ workspaceId, moves }) => ({
        url: `/workspaces/${workspaceId}/nodes/batch-move`,
        method: "POST",
        data: moves
      }),
      async onQueryStarted({ workspaceId, moves }, { dispatch, queryFulfilled, getState }) {
        // NOTE: Entity upserts (hasTasks, parent changes, orderKey) are done IMMEDIATELY
        const state = getState() as RootState;

        moves.forEach((move) => {
          const itemId = move.itemId;
          const targetParentId = move.targetParentId;

          if (move.itemType === "ProjectFolder" && targetParentId) {
            const folder = folderSelectors.selectById(state, itemId);
            if (folder) {
              dispatch(
                hierarchyApi.util.updateQueryData("getNodeFolders", { workspaceId, nodeId: targetParentId, cursor: null }, (draft) => {
                  if (!draft?.items) return;
                  const idx = draft.items.findIndex(f => f.id === itemId);
                  if (idx !== -1) {
                    draft.items[idx].orderKey = move.newOrderKey;
                  } else {
                    draft.items.push({ ...folder, orderKey: move.newOrderKey, spaceId: targetParentId });
                  }
                  draft.items.sort((a, b) => (a.orderKey ?? "").localeCompare(b.orderKey ?? ""));
                })
              );
            }
          }

          else if (move.itemType === "ProjectTask" && targetParentId) {
            const task = taskSelectors.selectById(state, itemId);
            if (task) {
              const isTargetSpace = !!spaceSelectors.selectById(state, targetParentId);
              const targetParentType = isTargetSpace ? "ProjectSpace" : "ProjectFolder";

              dispatch(
                hierarchyApi.util.updateQueryData(
                  "getNodeTasks",
                  { workspaceId, nodeId: targetParentId, parentType: targetParentType as EntityLayerType, cursor: null },
                  (draft) => {
                    if (!draft?.items) return;
                    const idx = draft.items.findIndex(t => t.id === itemId);
                    if (idx !== -1) {
                      draft.items[idx].orderKey = move.newOrderKey;
                    } else {
                      draft.items.push({
                        ...task,
                        orderKey: move.newOrderKey,
                        projectSpaceId: isTargetSpace ? targetParentId : (folderSelectors.selectById(state, targetParentId)?.spaceId ?? task.projectSpaceId),
                        projectFolderId: isTargetSpace ? null : targetParentId
                      });
                    }
                    draft.items.sort((a, b) => (a.orderKey ?? "").localeCompare(b.orderKey ?? ""));
                  }
                )
              );
            }
          }
        });

        try {
          await queryFulfilled;
        } catch {
          // RTK Query automatically rolls back on failure
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
  useMoveItemMutation,
  useBatchMoveItemsMutation,
} = hierarchyApi;


import { useMemo } from "react";
import type { Priority } from "@/types/priority";

// --- CENTRAL RELATIONAL SELECTORS FOR COMPONENTS ---
export function useSpaces(workspaceId: string) {
  const selectForWorkspace = useMemo(() =>
    createSelector(
      [spaceSelectors.selectAll],
      (spaces) => spaces
        .filter(s => s.workspaceId === workspaceId)
        .sort((a, b) => (a.orderKey ?? "").localeCompare(b.orderKey ?? ""))
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
        .sort((a, b) => (a.orderKey ?? "").localeCompare(b.orderKey ?? ""))
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
          t.projectFolderId === parentId || 
          (t.projectSpaceId === parentId && !t.projectFolderId)
        )
        .sort((a, b) => (a.orderKey ?? "").localeCompare(b.orderKey ?? ""))
    );
  }, [parentId]);

  return useSelector(selectForThisParent);
}