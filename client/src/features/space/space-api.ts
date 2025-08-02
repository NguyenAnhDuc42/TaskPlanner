import apiClient from "@/lib/api-client";
import { CreateSpaceRequest, CreateSpaceResponse } from "./space-type";
import { TaskSummary } from "@/types/task";

// Note: This assumes you will create a 'POST /api/space' endpoint on the backend.
export const createSpace = async (data: CreateSpaceRequest): Promise<CreateSpaceResponse> => {
    const response = await apiClient.post<CreateSpaceResponse>("/space", data);
    return response.data;
};

export const getSpaceTasks = async (spaceId : string): Promise<TaskSummary[]> => {
    const response = await apiClient.get<TaskSummary[]>(`/space/${spaceId}/tasks`);
    return response.data;
};