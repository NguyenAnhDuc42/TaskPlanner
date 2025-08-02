import apiClient from "@/lib/api-client";
import { JoinWorkspaceResponse } from "./user-type";
import { CreateWorkspaceRequest } from "../workspace/workspacetype";
import { WorkspaceDetail } from "@/types/workspace";

export const joinWorkspace = async (joinCode: string): Promise<JoinWorkspaceResponse> => {
    try {
        const rep = await apiClient.post<JoinWorkspaceResponse>(`/user/join-workspace`, { joinCode });
        return rep.data;
    } catch (error) {
        throw error;
    }
}
export const createWorkspace = async (data : CreateWorkspaceRequest) : Promise<string> =>{
    try {
        const rep = await apiClient.post<string>("/user/workspace",data);
        return rep.data;
    } catch (error) {
        throw error;
    }
}

export const getWorkspaces = async () : Promise<WorkspaceDetail[]> => {
    try {
        const rep = await apiClient.get<WorkspaceDetail[]>("/user/workspaces");
        return rep.data;
    } catch (error) {
        throw error;
    }
}
export const leaveWorkspace = async (workspaceId : string) : Promise<string> => {
    try {
        const rep = await apiClient.post<string>(`/user/leave-workspace`,workspaceId);
        return rep.data;
    } catch (error) {
        throw error;
    }
}

