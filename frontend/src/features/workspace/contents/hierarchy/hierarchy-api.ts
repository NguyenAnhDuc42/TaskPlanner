import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo } from "react";
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
    staleTime: 1000 * 60 * 5,
  });
}

export function useNodeTasks(workspaceId: string, nodeId: string, parentType: 'Folder' | 'Space') {
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
  });
}

export function useCreateSpace(workspaceId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateSpaceRequest) => api.post(`/workspaces/${workspaceId}/spaces`, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: hierarchyKeys.detail(workspaceId) });
    },
  });
}

export function useUpdateSpace(workspaceId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateSpaceRequest) => api.patch(`/workspaces/${workspaceId}/spaces/${data.spaceId}`, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: hierarchyKeys.detail(workspaceId) });
    },
  });
}

export function useCreateFolder(workspaceId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateFolderRequest) => api.post(`/workspaces/${workspaceId}/folders`, data),
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
    onSuccess: () => {
      // Invalidate everything to ensure ordering is correct across the tree
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
