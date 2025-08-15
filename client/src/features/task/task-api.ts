import apiClient from "@/lib/api-client";
import {  CreateTaskRequest, CreateTaskResponse} from "./task-type";
import { TasksSummary } from "@/types/task";
import { TaskQuery } from "@/types/query/taskquery";
import { PagedResult } from "@/types/pagedresult";

export const CreateTask  = async (data : CreateTaskRequest) : Promise<CreateTaskResponse> =>{
    const response = await apiClient.post<CreateTaskResponse>("/task", data);
    return response.data;
}


export const GetTasks = async (query: TaskQuery): Promise<PagedResult<TasksSummary>> => {
  const response = await apiClient.get<PagedResult<TasksSummary>>('/tasks', {
    params: query,
  });
  return response.data;
};