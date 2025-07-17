import apiClient from "@/lib/api-client";
import { CreateListRequest, CreateListResponse, CreateTaskInListRequest, GetListTasksRequest, TaskLineList } from "./list-type";
import { CreateTaskResponse } from "../task/task-type";

export const createList = async (data: CreateListRequest): Promise<CreateListResponse> => {
    const response = await apiClient.post<CreateListResponse>("/list", data);
    return response.data;
};
export const CreateTaskInList  = async (data : CreateTaskInListRequest) : Promise<CreateTaskResponse> =>{
    const response = await apiClient.post<CreateTaskResponse>(`/list/createtask`, data);
    return response.data;
}

export const getListTasks = async ({ listId }: GetListTasksRequest): Promise<TaskLineList> => {
    const response = await apiClient.get<TaskLineList>(`/list/${listId}/tasks`);
    return response.data;
};