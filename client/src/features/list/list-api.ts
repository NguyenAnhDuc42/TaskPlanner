import apiClient from "@/lib/api-client";
import { TaskList } from "../space/space-type";
import { CreateListRequest, CreateListResponse, GetListTasksRequest } from "./list-type";

export const createList = async (data: CreateListRequest): Promise<CreateListResponse> => {
    const response = await apiClient.post<CreateListResponse>("/list", data);
    return response.data;
};

export const getListTasks = async ({ listId }: GetListTasksRequest): Promise<TaskList> => {
    const response = await apiClient.get<TaskList>(`/list/${listId}/tasks`);
    return response.data;
};