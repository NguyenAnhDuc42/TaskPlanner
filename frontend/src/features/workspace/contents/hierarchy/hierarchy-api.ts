import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo } from "react";
import { type EntityLayerType } from "@/types/entity-layer-type";
import type { CreateFolderRequest, CreateSpaceRequest, CreateTaskRequest, MoveItemRequest, NodeTasksResponse, UpdateFolderRequest, UpdateSpaceRequest, WorkspaceHierarchy } from "./hierarchy-type";
import { hierarchyKeys } from "./hierarchy-keys";
import { api } from "@/lib/api-client";

export function useHierarchy(workspaceId: string) {
  return useQuery({
    queryKey: hierarchyKeys.detail(workspaceId),
    queryFn: async () => {
      const { data } = await api.get<WorkspaceHierarchy>(`/workspaces/${workspaceId}/hierarchy`);
      return data;
    },
    enabled: !!workspaceId,
    staleTime: 1000 * 60 * 5, // 5 minutes
    gcTime: 1000 * 60 * 30, // 30 minutes
  });
}

export function useNodeTasks(workspaceId: string, nodeId: string, parentType: EntityLayerType) {
  return useInfiniteQuery({
    queryKey: hierarchyKeys.nodeTasks(workspaceId, nodeId),
    queryFn: async ({ pageParam }) => {
      const { data } = await api.get<NodeTasksResponse>(
        `/workspaces/${workspaceId}/hierarchy/nodes/${nodeId}/tasks`,
        {
          params: {
            parentType,
            cursorOrderKey: pageParam?.orderKey,
            cursorTaskId: pageParam?.taskId,
          },
        }
      );
      return data;
    },
    initialPageParam: null as { orderKey: string, taskId: string } | null,
    getPreviousPageParam: () => undefined,
    getNextPageParam: (lastPage) => 
      lastPage.hasMore ? { orderKey: lastPage.nextCursorOrderKey!, taskId: lastPage.nextCursorTaskId! } : undefined,
    enabled: !!nodeId && !!workspaceId,
    staleTime: 1000 * 60 * 5, // 5 minutes
    gcTime: 1000 * 60 * 30, // 30 minutes
  });
}

export function useCreateSpace(workspaceId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateSpaceRequest) => api.post(`/spaces`, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: hierarchyKeys.detail(workspaceId) });
    },
  });
}

export function useUpdateSpace(workspaceId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateSpaceRequest) => api.patch(`/spaces/${data.spaceId}`, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: hierarchyKeys.detail(workspaceId) });
    },
  });
}

export function useCreateFolder(workspaceId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateFolderRequest) => api.post(`/folders`, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: hierarchyKeys.detail(workspaceId) });
    },
  });
}
export function useCreateTask(workspaceId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateTaskRequest) => api.post(`/tasks`, data),
    onSuccess: (_, variables) => {
      // Invalidate the parent's task list
      queryClient.invalidateQueries({ queryKey: hierarchyKeys.nodeTasks(workspaceId, variables.parentId) });
      // Also invalidate hierarchy to update task counts if any
      queryClient.invalidateQueries({ queryKey: hierarchyKeys.detail(workspaceId) });
    },
  });
}

export function useUpdateFolder(workspaceId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateFolderRequest) => api.patch(`/workspaces/${workspaceId}/folders/${data.folderId}`, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: hierarchyKeys.detail(workspaceId) });
    },
  });
}

export function useMoveItem(workspaceId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: MoveItemRequest) => api.post(`/workspaces/${workspaceId}/hierarchy/move`, data),
    onMutate: async (moveRequest) => {
      // Cancel any outgoing refetches (so they don't overwrite our optimistic update)
      await queryClient.cancelQueries({ queryKey: hierarchyKeys.detail(workspaceId) });

      // Snapshot the previous value
      const previousHierarchy = queryClient.getQueryData<WorkspaceHierarchy>(hierarchyKeys.detail(workspaceId));

      // Optimistically update to the new value
      if (previousHierarchy) {
        queryClient.setQueryData<WorkspaceHierarchy>(hierarchyKeys.detail(workspaceId), (old) => {
          if (!old) return old;
          
          const newHierarchy = JSON.parse(JSON.stringify(old)) as WorkspaceHierarchy;
          const { itemId, itemType, targetParentId, nextItemOrderKey } = moveRequest;

          if (itemType === "ProjectSpace") {
            const spaces = newHierarchy.spaces || [];
            const activeIndex = spaces.findIndex(s => s.id === itemId);
            if (activeIndex !== -1) {
              const [removed] = spaces.splice(activeIndex, 1);
              // Find target index based on nextItemOrderKey
              const overIndex = nextItemOrderKey 
                ? spaces.findIndex(s => s.orderKey === nextItemOrderKey) 
                : spaces.length;
              spaces.splice(overIndex === -1 ? spaces.length : overIndex, 0, removed);
            }
          } 
          else if (itemType === "ProjectFolder") {
            let folderToMove: any = null;
            const spaces = newHierarchy.spaces || [];
            for (const space of spaces) {
              const folders = space.folders || [];
              const idx = folders.findIndex(f => f.id === itemId);
              if (idx !== -1) {
                [folderToMove] = folders.splice(idx, 1);
                break;
              }
            }
            
            if (folderToMove && targetParentId) {
              const targetSpace = (newHierarchy.spaces || []).find(s => s.id === targetParentId);
              if (targetSpace) {
                const targetFolders = targetSpace.folders || [];
                // Find target index in targetSpace folders
                const overIndex = nextItemOrderKey 
                  ? targetFolders.findIndex(f => f.orderKey === nextItemOrderKey) 
                  : targetFolders.length;
                targetFolders.splice(overIndex === -1 ? targetFolders.length : overIndex, 0, folderToMove);
                targetSpace.folders = targetFolders; // Ensure assignment if was empty
              }
            }
          }

          return newHierarchy;
        });
      }

      return { previousHierarchy };
    },
    onError: (_err, _newTodo, context) => {
      if (context?.previousHierarchy) {
        queryClient.setQueryData(hierarchyKeys.detail(workspaceId), context.previousHierarchy);
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: hierarchyKeys.all });
    },
  });
}

export function useEntityInfo(workspaceId: string, entityId: string | undefined) {
  const { data: hierarchy } = useHierarchy(workspaceId);

  return useMemo(() => {
    if (!entityId || !hierarchy) return null;

    // Search Spaces
    const space = hierarchy.spaces.find((s) => s.id === entityId);
    if (space) return { id: space.id, name: space.name, icon: space.icon, color: space.color, type: "space" };

    // Search Folders
    for (const s of hierarchy.spaces) {
      const folder = s.folders.find((f) => f.id === entityId);
      if (folder) return { id: folder.id, name: folder.name, icon: folder.icon, color: folder.color, type: "folder" };
    }

    return null;
  }, [hierarchy, entityId]);
}
