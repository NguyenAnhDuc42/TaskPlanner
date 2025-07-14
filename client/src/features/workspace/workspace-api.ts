import apiClient from "@/lib/api-client";
import { CreateWorkspaceRequest, CreateWorkspaceResponse, GetHierarchyRequest, Hierarchy, Workspaces,  } from "./workspacetype";
 
export const CreateWorkspace = async (data: CreateWorkspaceRequest) : Promise<CreateWorkspaceResponse> => {
    try {
        const rep = await apiClient.post<CreateWorkspaceResponse>("/workspace",data);
        return rep.data;
    } catch (error) {
        throw error;
    }
}
export const SidebarWorkspaces = async () : Promise<Workspaces> => {
    try {
        const rep = await apiClient.get<Workspaces>("/workspace/sidebar");
        return rep.data;
    } catch (error) {
        throw error;
    }
}
export const GetHierarchy = async (data : GetHierarchyRequest) : Promise<Hierarchy> => {
    try {
        const rep = await apiClient.get<Hierarchy>(`/workspace/${data.id}/hierarchy`);
        return rep.data;
    } catch (error) {
        throw error;
    }
}