import apiClient from "@/lib/api-client";

import { TasksMetadata, TasksSummary } from "@/types/task";
import { TaskQuery } from "@/types/query/taskquery";
import { PagedResult } from "@/types/pagedresult";



export const GetTasks = async (query: TaskQuery): Promise<PagedResult<TasksSummary>> => {
  const response = await apiClient.get<PagedResult<TasksSummary>>('/tasks', {
    params: query,
  });
  return response.data;
};

export const GetTasksMetadata = async (query: TaskQuery): Promise<TasksMetadata> => {
  const response = await apiClient.get<TasksMetadata>('/tasks/metadata', {
    params: query,
  });
  return response.data;
}

