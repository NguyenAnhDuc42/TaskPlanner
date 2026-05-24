import {
  QueryClient,
  useInfiniteQuery,
  useMutation,
  useQueryClient,
} from "@tanstack/react-query";
import { type EntityLayerType } from "@/types/entity-layer-type";
import type { SpaceRecord, FolderRecord, TaskRecord } from "@/types/projects";
import type { PagedResult } from "@/types/paged-result";
import { hierarchyKeys } from "./hierarchy-keys";
import { api } from "@/lib/api-client";

// Request models (We will define these manually for now)
export interface CreateSpaceRequest {
  name: string;
  isPrivate: boolean;
  color?: string;
  icon?: string;
}

export interface CreateFolderRequest {
  projectSpaceId: string;
  name: string;
  isPrivate?: boolean;
  color?: string;
  icon?: string;
  statusId?: string | null;
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

// --- Structure Queries (Sidebar) ---

export function useNodeSpaces(workspaceId: string) {
  return useInfiniteQuery({
    queryKey: hierarchyKeys.detail(workspaceId),
    queryFn: async ({ pageParam }) => {
      const { data } = await api.get<PagedResult<SpaceRecord>>(
        `/workspaces/${workspaceId}/nodes/spaces`,
        { params: { cursor: pageParam } }
      );
      return data;
    },
    initialPageParam: null as string | null,
    getNextPageParam: (lastPage) => lastPage.nextCursor || undefined,
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
  return useInfiniteQuery({
    queryKey: hierarchyKeys.nodeFolders(workspaceId, nodeId),
    queryFn: async ({ pageParam }) => {
      const { data } = await api.get<PagedResult<FolderRecord>>(
        `/workspaces/${workspaceId}/nodes/${nodeId}/folders`,
        { params: { cursor: pageParam } }
      );
      return data;
    },
    initialPageParam: null as string | null,
    getNextPageParam: (lastPage) => lastPage.nextCursor || undefined,
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
      const { data } = await api.get<PagedResult<TaskRecord>>(
        `/workspaces/${workspaceId}/nodes/${nodeId}/tasks`,
        {
          params: {
            parentType,
            cursor: pageParam,
          },
        },
      );
      return data;
    },
    initialPageParam: null as string | null,
    getNextPageParam: (lastPage) => lastPage.nextCursor || undefined,
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
      api.post(`/workspaces/${workspaceId}/nodes/move`, data),
    onSuccess: (_, variables) => {
       if (variables.itemType === "ProjectTask") {
         if (variables.targetParentId) {
           queryClient.invalidateQueries({ queryKey: hierarchyKeys.nodeTasks(workspaceId, variables.targetParentId) });
         }
         if (variables.sourceParentId) {
           queryClient.invalidateQueries({ queryKey: hierarchyKeys.nodeTasks(workspaceId, variables.sourceParentId) });
         }
       } else if (variables.itemType === "ProjectFolder") {
         if (variables.targetParentId) {
           queryClient.invalidateQueries({ queryKey: hierarchyKeys.nodeFolders(workspaceId, variables.targetParentId) });
         }
         if (variables.sourceParentId) {
           queryClient.invalidateQueries({ queryKey: hierarchyKeys.nodeFolders(workspaceId, variables.sourceParentId) });
         }
       } else if (variables.itemType === "ProjectSpace") {
         queryClient.invalidateQueries({ queryKey: hierarchyKeys.detail(workspaceId) });
       }
    }
  });
}

// --- Prefetch Helpers ---

export const prefetchNodeFolders = async (
  queryClient: QueryClient,
  workspaceId: string,
  nodeId: string,
) => {
  await queryClient.prefetchInfiniteQuery({
    queryKey: hierarchyKeys.nodeFolders(workspaceId, nodeId),
    queryFn: async () => {
      const { data } = await api.get<PagedResult<FolderRecord>>(
        `/workspaces/${workspaceId}/nodes/${nodeId}/folders`,
        { params: { cursor: null } }
      );
      return data;
    },
    initialPageParam: null,
    staleTime: 1000 * 60 * 5,
  });
};

export const prefetchNodeTasks = async (
  queryClient: QueryClient,
  workspaceId: string,
  nodeId: string,
  parentType: EntityLayerType,
) => {
  await queryClient.prefetchInfiniteQuery({
    queryKey: hierarchyKeys.nodeTasks(workspaceId, nodeId),
    queryFn: async () => {
      const { data } = await api.get<PagedResult<TaskRecord>>(
        `/workspaces/${workspaceId}/nodes/${nodeId}/tasks`,
        { params: { parentType, cursor: null } },
      );
      return data;
    },
    initialPageParam: null,
    staleTime: 1000 * 60 * 5,
  });
};
