import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createList, CreateTaskInList, getListTasks } from "./list-api";
import { WORKSPACE_KEYS } from "../workspace/workspace-hooks";
import { toast } from "sonner";
import { ErrorResponse } from "@/types/responses/error-response";
import { TaskLineList } from "./list-type";

export const LIST_KEYS = {
    all: ["lists"] as const,
    tasks: (listId: string) => [...LIST_KEYS.all, listId, "tasks"] as const,
} as const;

export function useCreateList() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: createList,
        onSuccess: (data, variables) => {
            toast.success(data.message || "List created successfully!");
            queryClient.invalidateQueries({
                queryKey: WORKSPACE_KEYS.hierarchy(variables.workspaceId)
            });
        },
        onError: (error: ErrorResponse) => {
            toast.error(error.detail || error.title || "Failed to create list.");
        }
    });
}
export function useCreateTaskInList() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: CreateTaskInList,
    onSuccess: (data,variables) => {
      toast.success(data.message || "Task created successfully!");
      queryClient.invalidateQueries({
        queryKey: LIST_KEYS.tasks(variables.listId),
      });
    },
    onError: (error: ErrorResponse) => {
      toast.error(error.detail || error.title || "Failed to create task.");
    },
  });
}

export function useListTasks(listId: string | undefined) {
    return useQuery<TaskLineList, ErrorResponse>({
        queryKey: listId ? LIST_KEYS.tasks(listId) : ["disabled-list-tasks-query"],
        queryFn: () => {
            if (!listId) throw new Error("List ID is required to fetch tasks.");
            return getListTasks({ listId });
        },
        enabled: !!listId,
    });
}