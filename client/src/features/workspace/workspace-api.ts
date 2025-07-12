import apiClient from "@/lib/api-client";
import { CreateWorkspaceRequest, CreateWorkspaceResponse, SidebarWorkspacesResponse } from "./workspacetype";
 
export const CreateWorkspace = async (data: CreateWorkspaceRequest) : Promise<CreateWorkspaceResponse> => {
    try {
        const rep = await apiClient.post<CreateWorkspaceResponse>("/workspace",data);
        return rep.data;
    } catch (error) {
        throw error;
    }
}
export const SidebarWorkspaces = async () : Promise<SidebarWorkspacesResponse> => {
    try {
        const rep = await apiClient.get<SidebarWorkspacesResponse>("/workspace/sidebar");
        return rep.data;
    } catch (error) {
        throw error;
    }
}