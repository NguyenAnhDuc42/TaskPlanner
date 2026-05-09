import {
  useInfiniteQuery,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { type EntityLayerType } from "@/types/entity-layer-type";
import type {
  CreateFolderRequest,
  CreateSpaceRequest,
  CreateTaskRequest,
  FolderHierarchy,
  NodeTasksResponse,
  WorkspaceHierarchy,
  MoveItemRequest,
} from "./hierarchy-type";
import { hierarchyKeys } from "./hierarchy-keys";
import { api } from "@/lib/api-client";

// --- Structure Queries (Sidebar) ---

export function useHierarchy(workspaceId: string) {
  return useQuery({
    queryKey: hierarchyKeys.detail(workspaceId),
    queryFn: async () => {
      const { data } = await api.get<WorkspaceHierarchy>(
        `/workspaces/${workspaceId}/hierarchy`,
      );
      return data;
    },
    enabled: !!workspaceId,
    staleTime: 1000 * 60 * 5,
    gcTime: 1000 * 60 * 30,
  });
}

export function useNodeFolders(
  workspaceId: string,
  nodeId: string,
  enabled: boolean = true,
) {
  return useQuery({
    queryKey: hierarchyKeys.nodeFolders(workspaceId, nodeId),
    queryFn: async () => {
      const { data } = await api.get<FolderHierarchy[]>(
        `/workspaces/${workspaceId}/hierarchy/nodes/${nodeId}/folders`,
      );
      return data;
    },
    enabled: !!workspaceId && !!nodeId && enabled,
    staleTime: 1000 * 60 * 5,
  });
}

export function useNodeTasks(
  workspaceId: string,
  nodeId: string,
  parentType: EntityLayerType,
) {
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
        },
      );
      return data;
    },
    initialPageParam: null as { orderKey: string; taskId: string } | null,
    getPreviousPageParam: () => undefined,
    getNextPageParam: (lastPage) =>
      lastPage.hasMore
        ? {
            orderKey: lastPage.nextCursorOrderKey!,
            taskId: lastPage.nextCursorTaskId!,
          }
        : undefined,
    enabled: !!nodeId && !!workspaceId,
    staleTime: 1000 * 60 * 5,
    gcTime: 1000 * 60 * 30,
  });
}

// --- Creation Mutations ---

export function useCreateSpace(workspaceId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateSpaceRequest) => 
      api.post(`/spaces`, data),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: hierarchyKeys.detail(workspaceId),
      });
    },
  });
}

export function useCreateFolder(workspaceId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateFolderRequest) => 
      api.post(`/folders`, data),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: hierarchyKeys.detail(workspaceId),
      });
    },
  });
}

export function useCreateTask(workspaceId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateTaskRequest) => 
      api.post(`/tasks`, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: hierarchyKeys.nodeTasks(workspaceId, variables.parentId),
      });
      queryClient.invalidateQueries({
        queryKey: hierarchyKeys.detail(workspaceId),
      });
    },
  });
}

// --- Delete Mutations ---

export function useDeleteSpace(workspaceId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (spaceId: string) => 
      api.delete(`/spaces/${spaceId}`),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: hierarchyKeys.detail(workspaceId),
      });
    },
  });
}

export function useDeleteFolder(workspaceId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (folderId: string) => 
      api.delete(`/folders/${folderId}`),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: hierarchyKeys.detail(workspaceId),
      });
    },
  });
}

export function useDeleteTask(workspaceId: string, parentId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (taskId: string) => 
      api.delete(`/tasks/${taskId}`),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: hierarchyKeys.nodeTasks(workspaceId, parentId),
      });
      queryClient.invalidateQueries({
        queryKey: hierarchyKeys.detail(workspaceId),
      });
    },
  });
}

// --- Movement Logic (PREMIUM OPTIMISTIC) ---

export function useMoveItem(workspaceId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: MoveItemRequest) =>
      api.post(`/workspaces/${workspaceId}/hierarchy/move`, data),
    onMutate: async (moveRequest) => {
      const { itemId, itemType, nextItemOrderKey, newOrderKey } = moveRequest;

      await queryClient.cancelQueries({ queryKey: hierarchyKeys.detail(workspaceId) });
      await queryClient.cancelQueries({ queryKey: hierarchyKeys.all });

      const previousHierarchy = queryClient.getQueryData<WorkspaceHierarchy>(hierarchyKeys.detail(workspaceId));
      
      // Optimistic updates for structural changes
      if (previousHierarchy) {
        queryClient.setQueryData<WorkspaceHierarchy>(hierarchyKeys.detail(workspaceId), (old) => {
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
              newHierarchy.spaces = spaces.sort((a, b) => (a.orderKey < b.orderKey ? -1 : a.orderKey > b.orderKey ? 1 : 0));
            }
          }
          // Note: Folder/Task moves across nodes are handled by onSettled invalidation 
          // because they often involve cross-query updates (NodeFolders, NodeTasks).
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

// --- Prefetch Helpers ---

export const prefetchNodeFolders = async (
  queryClient: any,
  workspaceId: string,
  nodeId: string,
) => {
  await queryClient.prefetchQuery({
    queryKey: hierarchyKeys.nodeFolders(workspaceId, nodeId),
    queryFn: async () => {
      const { data } = await api.get<FolderHierarchy[]>(
        `/workspaces/${workspaceId}/hierarchy/nodes/${nodeId}/folders`,
      );
      return data;
    },
    staleTime: 1000 * 60 * 5,
  });
};

export const prefetchNodeTasks = async (
  queryClient: any,
  workspaceId: string,
  nodeId: string,
  parentType: EntityLayerType,
) => {
  await queryClient.prefetchInfiniteQuery({
    queryKey: hierarchyKeys.nodeTasks(workspaceId, nodeId),
    queryFn: async () => {
      const { data } = await api.get<NodeTasksResponse>(
        `/workspaces/${workspaceId}/hierarchy/nodes/${nodeId}/tasks`,
        { params: { parentType, cursorOrderKey: null, cursorTaskId: null } },
      );
      return data;
    },
    initialPageParam: null,
    staleTime: 1000 * 60 * 5,
  });
};
