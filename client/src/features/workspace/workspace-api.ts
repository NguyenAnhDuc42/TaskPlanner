import apiClient from "@/lib/api-client";
import { AddMembersBody, AddMembersResponse, CreateWorkspaceRequest, CreateWorkspaceResponse, GroupWorkspace, Hierarchy, UpdateMembersBody,  } from "./workspacetype";
import { UserSummary } from "@/types/user";

 
export const CreateWorkspace = async (data: CreateWorkspaceRequest) : Promise<CreateWorkspaceResponse> => {
        const rep = await apiClient.post<CreateWorkspaceResponse>("/workspace",data);
        return rep.data;
}

//sidebar
export const SidebarWorkspaces = async (workspaceId : string) : Promise<GroupWorkspace> => {
        const rep = await apiClient.get<GroupWorkspace>(`/workspace/${workspaceId}/sidebar`);
        return rep.data;
}
export const GetHierarchy = async (workspaceId : string) : Promise<Hierarchy> => {
        const rep = await apiClient.get<Hierarchy>(`/workspace/${workspaceId}/hierarchy`);
        return rep.data;
}

//members

export const GetMembers = async (workspaceId: string): Promise<UserSummary[]> => {
  const response = await apiClient.get<UserSummary[]>(
    `/workspace/${workspaceId}/members`
  );
  return response.data ?? [];
};

export const AddMembers = async (workspaceId: string, body:AddMembersBody) : Promise<AddMembersResponse> => {
        const rep = await apiClient.post<AddMembersResponse>(`/workspace/${workspaceId}/members`, body);
        return rep.data;
}

export const UpdateMembers = async (workspaceId:string, body: UpdateMembersBody) : Promise<string> => {
        const response = await apiClient.put<string>(`/workspace/${workspaceId}/members`, body);
        return response.data;
}
export const DeleteMembers = async (workspaceId: string, memberIds: string[]) : Promise<string> =>{
        const rep = await apiClient.post<string>(`/workspace/${workspaceId}/delete-members`, memberIds);
        return rep.data;
}




