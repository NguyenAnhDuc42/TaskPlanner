import { useInfiniteQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { ErrorResponse } from "@/types/responses/error-response";
import { CreateTask, GetTasks } from "./task-api";
import { LIST_KEYS } from "../list/list-hooks";
import { TaskQuery } from "@/types/query/taskquery";

export const TASK_KEYS = {
  all: ["tasks"] as const,
  details: () => [...TASK_KEYS.all, "detail"] as const,
  detail: (id: string) => [...TASK_KEYS.details(), id] as const,
} as const;

export function useCreateTask() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: CreateTask,
    onSuccess: (data, variables) => {
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

export const useInfiniteTasks = (query: TaskQuery) => {
  // The call to useInfiniteQuery is safely inside our custom hook.
  // This follows the Rules of Hooks.
  const queryResult = useInfiniteQuery({
    // The queryKey uniquely identifies this query based on its filters.
    queryKey: ['tasks', query],

    // The queryFn calls our API function, passing the cursor.
    queryFn: ({ pageParam = null}) => GetTasks({ ...query, cursor: pageParam}),

    // This function tells the hook how to get the cursor for the next page.
    getNextPageParam: (lastPage) => {
      return lastPage.hasNextPage ? lastPage.nextCursor : undefined;
    },

    // The first page has no cursor.
    initialPageParam: null as string | null,
  });

  return queryResult;
};