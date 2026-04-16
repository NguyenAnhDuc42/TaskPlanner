import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo } from "react";
import { type EntityLayerType } from "@/types/entity-layer-type";
import type { CreateFolderRequest, CreateSpaceRequest, CreateTaskRequest, FolderHierarchy, MoveItemRequest, NodeTasksResponse, TaskHierarchy, UpdateFolderRequest, UpdateSpaceRequest, UpdateTaskRequest, WorkspaceHierarchy } from "./hierarchy-type";
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

export function useNodeFolders(workspaceId: string, nodeId: string, enabled: boolean = true) {
  return useQuery({
    queryKey: [...hierarchyKeys.detail(workspaceId), nodeId, "folders"],
    queryFn: async () => {
      const { data } = await api.get<FolderHierarchy[]>(`/workspaces/${workspaceId}/hierarchy/nodes/${nodeId}/folders`);
      return data;
    },
    enabled: !!workspaceId && !!nodeId && enabled,
    staleTime: 1000 * 60 * 5,
  });
}

export const prefetchNodeFolders = async (queryClient: any, workspaceId: string, nodeId: string) => {
  await queryClient.prefetchQuery({
    queryKey: [...hierarchyKeys.detail(workspaceId), nodeId, "folders"],
    queryFn: async () => {
      const { data } = await api.get<FolderHierarchy[]>(`/workspaces/${workspaceId}/hierarchy/nodes/${nodeId}/folders`);
      return data;
    },
    staleTime: 1000 * 60 * 5,
  });
};

export const prefetchNodeTasks = async (queryClient: any, workspaceId: string, nodeId: string, parentType: EntityLayerType) => {
  await queryClient.prefetchInfiniteQuery({
    queryKey: hierarchyKeys.nodeTasks(workspaceId, nodeId),
    queryFn: async () => {
      const { data } = await api.get<NodeTasksResponse>(
        `/workspaces/${workspaceId}/hierarchy/nodes/${nodeId}/tasks`,
        {
          params: {
            parentType,
            cursorOrderKey: null,
            cursorTaskId: null,
          },
        }
      );
      return data;
    },
    initialPageParam: null,
    staleTime: 1000 * 60 * 5,
  });
};

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
    mutationFn: (data: CreateSpaceRequest) => api.post(`/spaces/${workspaceId}`, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: hierarchyKeys.detail(workspaceId) });
    },
  });
}

export function useUpdateSpace(workspaceId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateSpaceRequest) => api.put(`/spaces/${workspaceId}/${data.spaceId}`, data),
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
    mutationFn: (data: UpdateFolderRequest) => api.put(`/folders/${data.folderId}`, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: hierarchyKeys.detail(workspaceId) });
    },
  });
}

export function useUpdateTask(workspaceId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateTaskRequest) => api.put(`/tasks/${data.taskId}`, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: hierarchyKeys.all });
    },
  });
}

export function useMoveItem(workspaceId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: MoveItemRequest) => api.post(`/workspaces/${workspaceId}/hierarchy/move`, data),
    onMutate: async (moveRequest) => {
      const { itemId, itemType, targetParentId, nextItemOrderKey, newOrderKey } = moveRequest;

      // 1. Cancel related queries
      await queryClient.cancelQueries({ queryKey: hierarchyKeys.detail(workspaceId) });
      await queryClient.cancelQueries({ queryKey: hierarchyKeys.all });

      // 2. Snapshot current state
      const previousHierarchy = queryClient.getQueryData<WorkspaceHierarchy>(hierarchyKeys.detail(workspaceId));
      
      // 3. OPTIMISTIC UPDATES (Partitioned - Crucial for lazy loading visibility)
      if (itemType === "ProjectFolder" && targetParentId) {
        const parentId = targetParentId;
        
        // Find and remove from the old list (if any)
        let movedItem: FolderHierarchy | undefined;
        queryClient.setQueriesData<FolderHierarchy[]>({ queryKey: [...hierarchyKeys.detail(workspaceId)] }, (old) => {
          if (!old || !Array.isArray(old)) return old; // Guard against WorkspaceHierarchy object
          const newFolders = [...old];
          const activeIdx = newFolders.findIndex(f => f.id === itemId);
          if (activeIdx !== -1) {
            [movedItem] = newFolders.splice(activeIdx, 1);
            return newFolders;
          }
          return old;
        });

        // Add to the new list if we found it (cross-space or same-space)
        if (movedItem) {
          queryClient.setQueryData<FolderHierarchy[]>([...hierarchyKeys.detail(workspaceId), parentId, "folders"], (old) => {
            if (!old) return old;
            const newFolders = [...old];
            if (newOrderKey) movedItem!.orderKey = newOrderKey;
            
            // Just add to end and sort, since fractional keys handle exact placement
            newFolders.push(movedItem!);
            return newFolders.sort((a, b) => a.orderKey.localeCompare(b.orderKey));
          });
        }
      }

      if (itemType === "ProjectTask" && targetParentId) {
        const nodeId = targetParentId;
        
        let movedTask: any = undefined;
        
        // Remove from any task list
        queryClient.setQueriesData<{ pages: NodeTasksResponse[], pageParams: any[] }>({ queryKey: [...hierarchyKeys.detail(workspaceId)] }, (old) => {
          if (!old || !old.pages) return old; // Guard against WorkspaceHierarchy or Folders
          const newPages = old.pages.map((page) => {
            const newTasks = [...(page.tasks || [])];
            const activeIdx = newTasks.findIndex((t) => t.id === itemId);
            if (activeIdx !== -1) {
              [movedTask] = newTasks.splice(activeIdx, 1);
              return { ...page, tasks: newTasks };
            }
            return page;
          });
          return { ...old, pages: newPages };
        });

        // Add to the new task list
        if (movedTask) {
          queryClient.setQueryData<{ pages: NodeTasksResponse[], pageParams: any[] }>(hierarchyKeys.nodeTasks(workspaceId, nodeId), (old) => {
            if (!old) return old;
            const newPages = [...old.pages];
            if (newPages.length > 0) {
              const newTasks = [...(newPages[0].tasks || [])];
              if (newOrderKey) movedTask.orderKey = newOrderKey;
              newTasks.push(movedTask);
              newPages[0] = { ...newPages[0], tasks: newTasks.sort((a, b) => a.orderKey.localeCompare(b.orderKey)) };
            }
            return { ...old, pages: newPages };
          });
        }
      }

      // 4. MAIN HIERARCHY UPDATE (Fallback/Global)
      if (previousHierarchy) {
        queryClient.setQueryData<WorkspaceHierarchy>(hierarchyKeys.detail(workspaceId), (old: WorkspaceHierarchy | undefined) => {
          if (!old) return old;
          const newHierarchy = JSON.parse(JSON.stringify(old)) as WorkspaceHierarchy;
          if (itemType === "ProjectSpace") {
            const spaces = newHierarchy.spaces || [];
            const activeIndex = spaces.findIndex(s => s.id === itemId);
            if (activeIndex !== -1) {
              const [removed] = spaces.splice(activeIndex, 1);
              if (newOrderKey) removed.orderKey = newOrderKey;
              const overIdx = nextItemOrderKey 
                ? spaces.findIndex(s => s.orderKey === nextItemOrderKey) 
                : spaces.length;
              spaces.splice(overIdx === -1 ? spaces.length : overIdx, 0, removed);
              newHierarchy.spaces = spaces.sort((a, b) => a.orderKey.localeCompare(b.orderKey));
            }
          }
          return newHierarchy;
        });
      }

      return { previousHierarchy };
    },
    onError: (_err, _variables, context) => {
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
