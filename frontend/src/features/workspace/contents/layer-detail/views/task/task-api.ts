import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useMemo } from "react";
import { api } from "@/lib/api-client";
import { workspaceKeys } from "@/features/main/query-keys";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import type { TaskDetailDto, UpdateTaskRequest } from "./task-types";

export const taskQueryOptions = {
  detail: (workspaceId: string, taskId: string) => ({
    queryKey: [...workspaceKeys.all, "task", taskId],
    queryFn: async () => {
      const { data } = await api.get<TaskDetailDto>(`/tasks/${taskId}`);
      return data;
    },
    enabled: !!workspaceId && !!taskId,
    staleTime: 3000,
  })
};

export function useTaskDetail(workspaceId: string, taskId: string, enabled = true) {
  const { registry } = useWorkspace();
  const query = useQuery({
    ...taskQueryOptions.detail(workspaceId, taskId),
    enabled: enabled && !!workspaceId && !!taskId
  });

  const enrichedData = useMemo(() => {
    if (!query.data) return null;
    const data = query.data;
    const status = data.statusId ? registry.statusMap[data.statusId] : null;
    const members = (data.assigneeIds || []).map((id: string) => registry.memberMap[id]).filter(Boolean);

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

export function useUpdateTask(onSuccess?: () => void) {
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
    onSuccess: () => {
      onSuccess?.();
    },
    onError: (_err, updates, context) => {
      if (context?.previousDetail) {
        queryClient.setQueryData([...workspaceKeys.all, "task", updates.taskId], context.previousDetail);
      }
    },
  });
}

export interface MoveTaskToStatusRequest {
  taskId: string;
  targetStatusId?: string;
  previousItemOrderKey?: string;
  nextItemOrderKey?: string;
  newOrderKey?: string;
}

export function useMoveTaskToStatus() {
  return useMutation({
    mutationFn: (data: MoveTaskToStatusRequest) => 
      api.post(`/tasks/${data.taskId}/move-status`, data),
    onSuccess: () => {
      // SignalR will handle invalidation, but we can also invalidate here if needed
    }
  });
}
