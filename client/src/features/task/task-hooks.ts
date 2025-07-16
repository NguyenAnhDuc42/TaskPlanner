import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { ErrorResponse } from "@/types/responses/error-response";   
import { CreateTask, DeleteTask, GetTask, UpdateTask } from "./task-api";
import { LIST_KEYS } from "../list/list-hooks";

export const TASK_KEYS = {
  all: ["tasks"] as const,
  details: () => [...TASK_KEYS.all, "detail"] as const,
  detail: (id: string) => [...TASK_KEYS.details(), id] as const,
} as const;

export function useGetTask(taskId?: string) {
  return useQuery({
    queryKey: TASK_KEYS.detail(taskId!),
    queryFn: () => GetTask(taskId!),
    enabled: !!taskId,
  });
}

export function useCreateTask() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: CreateTask,
    onSuccess: (data) => {
      toast.success(data.message || "Task created successfully!");
      queryClient.invalidateQueries({
        queryKey: LIST_KEYS.tasks(data.id),
      });
    },
    onError: (error: ErrorResponse) => {
      toast.error(error.detail || error.title || "Failed to create task.");
    },
  });
}

export function useUpdateTask() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: UpdateTask,
    onSuccess: (data) => {
      toast.success(data.message || "Task updated successfully!");
      queryClient.invalidateQueries({
        queryKey: TASK_KEYS.detail(data.task.id),
      });
      queryClient.invalidateQueries({
        queryKey: LIST_KEYS.tasks(data.task.listId),
      });
    },
    onError: (error: ErrorResponse) => {
      toast.error(error.detail || error.title || "Failed to update task.");
    },
  });
}

export function useDeleteTask() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: DeleteTask,
    onSuccess: (data, deletedTaskId) => {
      toast.success(data.message || "Task deleted.");
      queryClient.invalidateQueries({ queryKey: LIST_KEYS.all });
      queryClient.removeQueries({ queryKey: TASK_KEYS.detail(deletedTaskId) });
    },
    onError: (error: ErrorResponse) => {
      toast.error(error.detail || error.title || "Failed to delete task.");
    },
  });
}