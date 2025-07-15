import apiClient from "@/lib/api-client";
import { CreateSpaceRequest, CreateSpaceResponse, GetSpaceTasksRequest, TaskList } from "./space-type";

// Note: This assumes you will create a 'POST /api/space' endpoint on the backend.
export const createSpace = async (data: CreateSpaceRequest): Promise<CreateSpaceResponse> => {
    const response = await apiClient.post<CreateSpaceResponse>("/space", data);
    return response.data;
};

export const getSpaceTasks = async ({ spaceId }: GetSpaceTasksRequest): Promise<TaskList> => {
    const response = await apiClient.get<TaskList>(`/space/${spaceId}/tasks`);
    return response.data;
};