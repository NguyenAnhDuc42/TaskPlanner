import { useMutation, useQueryClient, useQuery } from "@tanstack/react-query";
import { useMemo } from "react";
import { api } from "@/lib/api-client";
import { hierarchyKeys } from "../hierarchy/hierarchy-keys";
import { workspaceKeys } from "@/features/main/query-keys";
import { EntityLayerType } from "@/types/entity-layer-type";
import { useWorkspace } from "../../context/workspace-provider";
import type { 
  UpdateSpaceRequest, 
  UpdateFolderRequest, 
  UpdateTaskRequest
} from "./layer-detail-types";

// --- Centralized Detail Query Options ---

export const entityQueryOptions = {
  detail: (workspaceId: string, entityId: string, type: EntityLayerType) => {
    const { urlSegment, keyType } = (() => {
      switch (type) {
        case EntityLayerType.ProjectSpace: return { urlSegment: "spaces", keyType: "space" as const };
        case EntityLayerType.ProjectFolder: return { urlSegment: "folders", keyType: "folder" as const };
        case EntityLayerType.ProjectTask: return { urlSegment: "tasks", keyType: "task" as const };
        default: return { urlSegment: "spaces", keyType: "space" as const };
      }
    })();

    return {
      queryKey: [...workspaceKeys.all, keyType, entityId],
      queryFn: async () => {
        const { data } = await api.get(`/${urlSegment}/${entityId}`);
        return data;
      },
      enabled: !!workspaceId && !!entityId,
      staleTime: 3000,
    };
  }
};

// --- Detail Hooks ---

export function useEntityDetail(workspaceId: string, entityId: string, type: EntityLayerType) {
  const { registry } = useWorkspace();

  const query = useQuery(entityQueryOptions.detail(workspaceId, entityId, type));

  const enrichedData = useMemo(() => {
    if (!query.data) return null;
    const data = query.data as any;
    const status = data.statusId ? registry.statusMap[data.statusId] : null;
    const memberIds = data.memberIds || data.assigneeIds || [];
    const members = memberIds.map((id: string) => registry.memberMap[id]).filter(Boolean);

    return {
      ...data,
      status,
      members,
      assignees: members
    };
  }, [query.data, registry]);

  return {
    data: enrichedData,
    isLoading: query.isLoading,
    isError: query.isError,
    error: query.error
  };
}

// --- Mutations ---

export function useUpdateSpace(workspaceId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateSpaceRequest) => api.put(`/spaces/${data.spaceId}`, data),
    onMutate: async (updates) => {
      await queryClient.cancelQueries({ queryKey: [...workspaceKeys.all, "space", updates.spaceId] });
      const previousDetail = queryClient.getQueryData([...workspaceKeys.all, "space", updates.spaceId]);
      if (previousDetail) {
        queryClient.setQueryData([...workspaceKeys.all, "space", updates.spaceId], (old: any) => ({
          ...old,
          ...updates
        }));
      }
      return { previousDetail };
    },
    onError: (_err, updates, context) => {
      if (context?.previousDetail) {
        queryClient.setQueryData([...workspaceKeys.all, "space", updates.spaceId], context.previousDetail);
      }
    },
    onSettled: (_data, _error, updates) => {
      // Trust SignalR & Optimistic Update for detail. Only invalidate hierarchy root if name/icon/color changed.
      queryClient.invalidateQueries({ queryKey: hierarchyKeys.detail(workspaceId) });
    },
  });
}

export function useUpdateFolder(workspaceId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateFolderRequest) => api.put(`/folders/${data.folderId}`, data),
    onMutate: async (updates) => {
      await queryClient.cancelQueries({ queryKey: [...workspaceKeys.all, "folder", updates.folderId] });
      const previousDetail = queryClient.getQueryData([...workspaceKeys.all, "folder", updates.folderId]);
      if (previousDetail) {
        queryClient.setQueryData([...workspaceKeys.all, "folder", updates.folderId], (old: any) => ({
          ...old,
          ...updates
        }));
      }
      return { previousDetail };
    },
    onError: (_err, updates, context) => {
      if (context?.previousDetail) {
        queryClient.setQueryData([...workspaceKeys.all, "folder", updates.folderId], context.previousDetail);
      }
    },
    onSettled: () => {
      // Trust SignalR for detail and node refresh.
    },
  });
}

export function useUpdateTask(workspaceId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateTaskRequest) => api.put(`/tasks/${data.taskId}`, data),
    onMutate: async (updates) => {
      await queryClient.cancelQueries({ queryKey: [...workspaceKeys.all, "task", updates.taskId] });
      const previousDetail = queryClient.getQueryData([...workspaceKeys.all, "task", updates.taskId]);
      if (previousDetail) {
        queryClient.setQueryData([...workspaceKeys.all, "task", updates.taskId], (old: any) => ({
          ...old,
          ...updates
        }));
      }
      return { previousDetail };
    },
    onError: (_err, updates, context) => {
      if (context?.previousDetail) {
        queryClient.setQueryData([...workspaceKeys.all, "task", updates.taskId], context.previousDetail);
      }
    },
    onSettled: () => {
      // Trust SignalR for detail and node refresh.
    },
  });
}
