import apiClient from "@/lib/api-client";
import { CreateSpaceRequest, CreateSpaceResponse, GetSpaceTasksRequest } from "./space-type";
import { TaskSumary } from "@/types/task";

// Note: This assumes you will create a 'POST /api/space' endpoint on the backend.
export const createSpace = async (data: CreateSpaceRequest): Promise<CreateSpaceResponse> => {
    const response = await apiClient.post<CreateSpaceResponse>("/space", data);
    return response.data;
};

export const getSpaceTasks = async ({ spaceId }: GetSpaceTasksRequest): Promise<TaskSumary[]> => {
    const response = await apiClient.get<TaskSumary[]>(`/space/${spaceId}/tasks`);
    return response.data;
};