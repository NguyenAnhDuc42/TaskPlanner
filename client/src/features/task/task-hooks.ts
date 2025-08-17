import { useInfiniteQuery, useQuery } from "@tanstack/react-query";

import { TaskQuery } from "@/types/query/taskquery";
import { GetTasks, GetTasksMetadata } from "./task-api";

export const TASK_KEYS = {
  all: ["tasks"] as const,
  details: () => [...TASK_KEYS.all, "detail"] as const,
  detail: (id: string) => [...TASK_KEYS.details(), id] as const,
} as const;



export const useInfiniteTasks = (query: TaskQuery) => {

  const queryResult = useInfiniteQuery({
    queryKey: ['tasks', query],
    queryFn: ({ pageParam = null}) => GetTasks({ ...query, cursor: pageParam}),
    getNextPageParam: (lastPage) => {
      return lastPage.hasNextPage ? lastPage.nextCursor : undefined;
    },
    initialPageParam: null as string | null,
  });

  return queryResult;
};

export const useTasksMetadata = (query: TaskQuery) =>{
  return useQuery({
    queryFn: () => GetTasksMetadata(query),
    queryKey: ['tasks-metadata', query],
  })
}