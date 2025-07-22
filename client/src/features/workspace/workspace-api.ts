import apiClient from "@/lib/api-client";
import { AddMembersResponse, CreateWorkspaceRequest, CreateWorkspaceResponse, GetHierarchyRequest, Hierarchy, Workspaces,  } from "./workspacetype";
import { Members } from "@/types/user";
 
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

export const GetMembers = async (workspaceId: string) : Promise<Members> => {
    try {
        const rep = await apiClient.get<Members>(`/workspace/${workspaceId}/members`);
        return rep.data;
    } catch (error) {
        throw error; 
    }
}

export const AddMembers = async (workspaceId: string, emails: string[]) : Promise<AddMembersResponse> => {
    try {
        const rep = await apiClient.post<AddMembersResponse>(`/workspace/${workspaceId}/members`, { emails });
        return rep.data;
    } catch (error) {
        throw error;
    }
}