import apiClient from "@/lib/api-client";
import { TaskList } from "../space/space-type";
import { CreateFolderRequest, CreateFolderResponse, GetFolderTasksRequest } from "./folder-type";

export const createFolder = async (data: CreateFolderRequest): Promise<CreateFolderResponse> => {
    const response = await apiClient.post<CreateFolderResponse>("/folder", data);
    return response.data;
};

export const getFolderTasks = async ({ folderId }: GetFolderTasksRequest): Promise<TaskList> => {
    const response = await apiClient.get<TaskList>(`/folder/${folderId}/tasks`);
    return response.data;
};