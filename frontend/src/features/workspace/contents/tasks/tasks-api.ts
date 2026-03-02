import { useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api-client";
import type {
  TaskDto,
  CreateTaskRequest,
  UpdateTaskRequest,
} from "./tasks-type";

export const useCreateTask = (workspaceId: string) => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: CreateTaskRequest) => {
      const response = await api.post<string>("/tasks", data, {
        headers: { "X-Workspace-Id": workspaceId },
      });
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["viewData"] });
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
      queryClient.invalidateQueries({ queryKey: ["viewData"] });
      queryClient.setQueryData(["task", updatedTask.id], updatedTask);
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
      queryClient.invalidateQueries({ queryKey: ["viewData"] });
    },
  });
};
