import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api-client";
import type {
  TaskDto,
  CreateTaskRequest,
  UpdateTaskRequest,
  TaskCreateListOption,
} from "./tasks-type";
import { tasksKeys } from "./tasks-keys";
import { viewsKeys } from "../views/views-keys";

export const useTaskCreateListOptions = (
  workspaceId: string,
  layerId: string,
  layerType: string,
  statusId?: string,
  enabled: boolean = true,
) => {
  return useQuery({
    queryKey: tasksKeys.createListOptions(
      workspaceId,
      layerId,
      layerType,
      statusId,
    ),
    queryFn: async () => {
      const response = await api.get<TaskCreateListOption[]>(
        "/lists/task-create-options",
        {
          params: { layerId, layerType, statusId },
          headers: { "X-Workspace-Id": workspaceId },
        },
      );
      return response.data;
    },
    enabled: enabled && !!workspaceId && !!layerId && !!layerType,
    staleTime: 60_000,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
  });
};

export const useCreateTask = (workspaceId: string) => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: CreateTaskRequest) => {
      const response = await api.post<TaskDto>("/tasks", data, {
        headers: { "X-Workspace-Id": workspaceId },
      });
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: viewsKeys.dataRoot() });
    },
  });
};

export const useUpdateTask = (workspaceId: string) => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: UpdateTaskRequest) => {
      const { taskId, ...body } = data;
      const response = await api.put<TaskDto>(`/tasks/${taskId}`, body, {
        headers: { "X-Workspace-Id": workspaceId },
      });
      return response.data;
    },
    onSuccess: (updatedTask) => {
      // Optimistic update or just invalidate
      queryClient.invalidateQueries({ queryKey: viewsKeys.dataRoot() });
      queryClient.setQueryData(tasksKeys.detail(updatedTask.id), updatedTask);
    },
  });
};

export const useDeleteTask = (workspaceId: string) => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (taskId: string) => {
      await api.delete(`/tasks/${taskId}`, {
        headers: { "X-Workspace-Id": workspaceId },
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: viewsKeys.dataRoot() });
    },
  });
};
