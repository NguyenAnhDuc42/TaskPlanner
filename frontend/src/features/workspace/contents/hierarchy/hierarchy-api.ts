import { workspaceApi } from "@/store/workspaceApi";
import { spaceSlice, folderSlice, taskSlice, spaceSelectors, folderSelectors, taskSelectors } from "@/store/entityStore";
import { useSelector } from "react-redux";
import type { RootState } from "@/store";
import type { SpaceRecord, FolderRecord, TaskRecord } from "@/types/projects";
import type { PagedResult } from "@/types/paged-result";
import type { EntityLayerType } from "@/types/entity-layer-type";
import { createSelector } from "@reduxjs/toolkit";
import { useMemo } from "react";
import type { Priority } from "@/types/priority";

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
          folderId: body.parentType === "ProjectFolder" ? body.parentId : null,
          spaceId:  body.parentType === "ProjectSpace"  ? body.parentId : null,
          icon: body.icon ?? null,
          color: body.color ?? null,
          statusId: body.statusId ?? null,
          priority: body.priority ?? null,
          startDate: body.startDate ?? null,
          dueDate: body.dueDate ?? null,
          createdAt: new Date().toISOString(),
        } as TaskRecord

        dispatch(taskSlice.actions.upsert(optimistic))

        try {
          const { data } = await queryFulfilled
          dispatch(taskSlice.actions.remove(tempId))
          
          const taskId = typeof data === 'string' ? data : (data as any).id || (data as any).value || tempId;
          const realTask: TaskRecord = {
            id: taskId,
            name: body.name,
            folderId: body.parentType === "ProjectFolder" ? body.parentId : null,
            spaceId:  body.parentType === "ProjectSpace"  ? body.parentId : null,
            icon: body.icon ?? null,
            color: body.color ?? null,
            statusId: body.statusId ?? null,
            priority: body.priority ?? null,
            startDate: body.startDate ?? null,
            dueDate: body.dueDate ?? null,
            createdAt: new Date().toISOString(),
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

    // moveItem: build.mutation<void, { workspaceId: string; body: MoveItemRequest }>({
    //   query: ({ workspaceId, body }) => ({ url: `/workspaces/${workspaceId}/nodes/move`, method: "POST", data: body }),
    //   async onQueryStarted({ workspaceId, body }, { dispatch, queryFulfilled, getState }) {
    //     const state = getState() as RootState;
    //     const itemId = body.itemId;

    //     const originalSpace = body.itemType === "ProjectSpace" ? spaceSelectors.selectById(state, itemId) : null;
    //     const originalFolder = body.itemType === "ProjectFolder" ? folderSelectors.selectById(state, itemId) : null;
    //     const originalTask = body.itemType === "ProjectTask" ? taskSelectors.selectById(state, itemId) : null;

    //     if (body.itemType === "ProjectSpace") {
    //       if (originalSpace && body.newOrderKey) {
    //         dispatch(spaceSlice.actions.upsert({ ...originalSpace, orderKey: body.newOrderKey }));
    //       }
    //     } 
    //     else if (body.itemType === "ProjectFolder") {
    //       if (originalFolder && body.newOrderKey) {
    //         dispatch(folderSlice.actions.upsert({ ...originalFolder, orderKey: body.newOrderKey, spaceId: body.targetParentId }));
 
    //         const targetSpace = spaceSelectors.selectById(state, body.targetParentId);
    //         if (targetSpace) {
    //           dispatch(spaceSlice.actions.upsert({ ...targetSpace, hasFolders: true }));
    //         }
    //       }
    //     } 
    //     else if (body.itemType === "ProjectTask") {
    //       if (originalTask && body.newOrderKey) {
    //         const isTargetSpace = body.targetParentType === "ProjectSpace";
    //         dispatch(taskSlice.actions.upsert({ 
    //           ...originalTask, 
    //           orderKey: body.newOrderKey, 
    //           spaceId: isTargetSpace ? body.targetParentId : originalTask.spaceId,
    //           folderId: isTargetSpace ? undefined : body.targetParentId
    //         }));

    
    //         if (isTargetSpace) {
    //           const targetSpace = spaceSelectors.selectById(state, body.targetParentId);
    //           if (targetSpace) {
    //             dispatch(spaceSlice.actions.upsert({ ...targetSpace, hasTasks: true }));
    //           }
    //         } else {
    //           const targetFolder = folderSelectors.selectById(state, body.targetParentId);
    //           if (targetFolder) {
    //             dispatch(folderSlice.actions.upsert({ ...targetFolder, hasTasks: true }));
    //           }
    //         }
    //       }
    //     }

    //     let patchResult: any;
    //     if (body.itemType === "ProjectFolder") {
    //       patchResult = dispatch(
    //         hierarchyApi.util.updateQueryData("getNodeFolders", { workspaceId, nodeId: body.targetParentId, cursor: null }, (draft) => {
    //           if (!draft || !draft.items) return;
              
    //           const itemIndex = draft.items.findIndex(f => f.id === itemId);
    //           if (itemIndex === -1) {
    //             const folder = originalFolder || folderSelectors.selectById(state, itemId);
    //             if (folder) {
    //               draft.items.push({ ...folder, orderKey: body.newOrderKey ?? "", spaceId: body.targetParentId });
    //             }
    //           } else {
    //             draft.items[itemIndex].orderKey = body.newOrderKey ?? draft.items[itemIndex].orderKey;
    //           }
    //           draft.items.sort((a, b) => (a.orderKey ?? "").localeCompare(b.orderKey ?? ""));
    //         })
    //       );
    //     }

    //     else if (body.itemType === "ProjectTask") {
    //       patchResult = dispatch(
    //         hierarchyApi.util.updateQueryData(
    //           "getNodeTasks", 
    //           { workspaceId, nodeId: body.targetParentId, parentType: body.targetParentType as EntityLayerType, cursor: null }, 
    //           (draft) => {
    //             if (!draft || !draft.items) return;

    //             const itemIndex = draft.items.findIndex(t => t.id === itemId);
    //             if (itemIndex === -1) {
    //               const task = originalTask || taskSelectors.selectById(state, itemId);
    //               if (task) {
    //                 const isTargetSpace = body.targetParentType === "ProjectSpace";
    //                 draft.items.push({
    //                   ...task,
    //                   orderKey: body.newOrderKey ?? "",
    //                   spaceId: isTargetSpace ? body.targetParentId : task.spaceId,
    //                   folderId: isTargetSpace ? undefined : body.targetParentId
    //                 });
    //               }
    //             } else {
    //               draft.items[itemIndex].orderKey = body.newOrderKey ?? draft.items[itemIndex].orderKey;
    //             }
    //             draft.items.sort((a, b) => (a.orderKey ?? "").localeCompare(b.orderKey ?? ""));
    //           }
    //         )
    //       );
    //     }

    //     try {
    //       await queryFulfilled;
    //     } catch {
    //       if (patchResult) {
    //         patchResult.undo();
    //       }
    //       if (originalSpace) {
    //         dispatch(spaceSlice.actions.upsert(originalSpace));
    //       }
    //       if (originalFolder) {
    //         dispatch(folderSlice.actions.upsert(originalFolder));
    //       }
    //       if (originalTask) {
    //         dispatch(taskSlice.actions.upsert(originalTask));
    //       }
    //     }
    //   }
    // }),

    batchMoveItems: build.mutation<void, { workspaceId: string; command: BatchMoveCommand }>({
      query: ({ workspaceId, command }) => ({
        url: `/workspaces/${workspaceId}/nodes/batch-move`,
        method: "POST",
        data: command
      }),
      async onQueryStarted({ workspaceId, command }, { dispatch, queryFulfilled, getState }) {
        // NOTE: Entity store upserts (hasTasks, parent changes, orderKey) happen immediately
        // in the handler files. Here we only patch the lazy-loaded node query caches.
        const state = getState() as RootState;
        const patches: { undo: () => void }[] = [];

        // Space cache patches
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
                draft.items.sort((a, b) => (a.orderKey ?? "").localeCompare(b.orderKey ?? ""));
              })
            );
            patches.push(patch);
          }
        });

        // Folder cache patches
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
                draft.items.sort((a, b) => (a.orderKey ?? "").localeCompare(b.orderKey ?? ""));
              })
            );
            patches.push(patch);

            // Remove from old parent folder query cache if moved to a different space
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

        // Task cache patches
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
                  draft.items.sort((a, b) => (a.orderKey ?? "").localeCompare(b.orderKey ?? ""));
                }
              )
            );
            patches.push(patch);

            // Remove from old container query cache if parent container changed
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
          // Rollback the optimistic query cache patches on failure
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
          t.folderId === parentId || 
          (t.spaceId === parentId && !t.folderId)
        )
        .sort((a, b) => (a.orderKey ?? "").localeCompare(b.orderKey ?? ""))
    );
  }, [parentId]);

  return useSelector(selectForThisParent);
}