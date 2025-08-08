import apiClient from "@/lib/api-client";
import { AddMembersBody, AddMembersResponse, CreateWorkspaceRequest, CreateWorkspaceResponse, GetHierarchyRequest, GroupWorkspace, Hierarchy, UpdateMembersBody,  } from "./workspacetype";
import { mapRoleFromApi } from "@/utils/role-utils";
import { FolderItems } from "@/types/folder";
import { ListItems } from "@/types/list";
import { TaskItems } from "@/types/task";
import { UserSummary } from "@/types/user";

 
export const CreateWorkspace = async (data: CreateWorkspaceRequest) : Promise<CreateWorkspaceResponse> => {
        const rep = await apiClient.post<CreateWorkspaceResponse>("/workspace",data);
        return rep.data;
}
export const SidebarWorkspaces = async (workspaceId : string) : Promise<GroupWorkspace> => {
        const rep = await apiClient.get<GroupWorkspace>(`/workspace/${workspaceId}/sidebar`);
        return rep.data;
}
export const GetHierarchy = async (data : GetHierarchyRequest) : Promise<Hierarchy> => {
        const rep = await apiClient.get<Hierarchy>(`/workspace/${data.id}/hierarchy`);
        return rep.data;
}

export const GetMembers = async (workspaceId: string) : Promise<UserSummary[]> => {
        const response = await apiClient.get<{ members: Array<Omit<UserSummary, 'role'> & { role: number }> }>(
            `/workspace/${workspaceId}/members`
        );
    
        return response.data.members.map(member => ({
            ...member,
            role: mapRoleFromApi(member.role)
        }));
}

export const AddMembers = async (workspaceId: string, body:AddMembersBody) : Promise<AddMembersResponse> => {
        const rep = await apiClient.post<AddMembersResponse>(`/workspace/${workspaceId}/members`, body);
        return rep.data;
}

export const UpdateMembers = async (workspaceId:string, body: UpdateMembersBody) : Promise<string> => {
        const response = await apiClient.put<string>(`/workspace/${workspaceId}/members`, body);
        return response.data;
}
export const DeleteMembers = async (workspaceId: string, memberIds: string[]) : Promise<string> =>{
    try {
        const rep = await apiClient.post<string>(`/workspace/${workspaceId}/delete-members`, memberIds);
        return rep.data;
    } catch (error) {
        throw error;
    }
}

export const GetDashboardFolders = async (workspaceId: string): Promise<FolderItems> => {
    try {
        const rep = await apiClient.get<FolderItems>(`/workspace/${workspaceId}/dashboard/folders`);
        return rep.data;
    } catch (error) {
        throw error;
    }
};

export const GetDashboardLists = async (workspaceId: string): Promise<ListItems> => {
        const rep = await apiClient.get<ListItems>(`/workspace/${workspaceId}/dashboard/lists`);
        return rep.data;
};

export const GetDashboardTasks = async (workspaceId: string): Promise<TaskItems> => {
        const rep = await apiClient.get<TaskItems>(`/workspace/${workspaceId}/dashboard/tasks`);
        return rep.data;
};