import apiClient from "@/lib/api-client";
import { JoinWorkspaceResponse } from "./user-type";

export const joinWorkspace = async (joinCode: string): Promise<JoinWorkspaceResponse> => {
    try {
        const rep = await apiClient.post<JoinWorkspaceResponse>(`/user/join-workspace`, { joinCode });
        return rep.data;
    } catch (error) {
        throw error;
    }
}
