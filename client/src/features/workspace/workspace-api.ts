import apiClient from "@/lib/api-client";
import { AddMembersBody, AddMembersResponse, CreateWorkspaceRequest, CreateWorkspaceResponse, GetHierarchyRequest, Hierarchy,  } from "./workspacetype";
import { mapRoleFromApi } from "@/utils/role-utils";
import { FolderItems } from "@/types/folder";
import { ListItems } from "@/types/list";
import { TaskItems } from "@/types/task";
import { WorkspaceSummary } from "@/types/workspace";
import { UserSummary } from "@/types/user";
 
export const CreateWorkspace = async (data: CreateWorkspaceRequest) : Promise<CreateWorkspaceResponse> => {
    try {
        const rep = await apiClient.post<CreateWorkspaceResponse>("/workspace",data);
        return rep.data;
    } catch (error) {
        throw error;
    }
}
export const SidebarWorkspaces = async () : Promise<WorkspaceSummary[]> => {
    try {
        const rep = await apiClient.get<WorkspaceSummary[]>("/workspace/sidebar");
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

export const GetMembers = async (workspaceId: string) : Promise<UserSummary[]> => {
    try {
        console.log('Fetching members for workspace:', workspaceId);
        const response = await apiClient.get<{ members: (Omit<UserSummary, 'role'> & { role: number })[] }>(`/workspace/${workspaceId}/members`);
        console.log('Raw API response:', response.data);
        
        if (!response.data.members) {
            console.error('No members array in response');
            return [];
        }
        
        const mappedMembers = response.data.members.map(member => ({
            ...member,
            role: mapRoleFromApi(member.role ?? 0)
        }));
        
        console.log('Mapped members:', mappedMembers);
        return mappedMembers;
    } catch (error) {
        console.error('Error in GetMembers:', error);
        throw error;
    }
}

export const AddMembers = async (workspaceId: string, body:AddMembersBody) : Promise<AddMembersResponse> => {
    try {
        const rep = await apiClient.post<AddMembersResponse>(`/workspace/${workspaceId}/members`, body);
        return rep.data;
    } catch (error) {
        throw error;
    }
}

export const GetDashboardFolders = async (workspaceId: string): Promise<FolderItems> => {
    try {
        const response = await apiClient.get<FolderItems>(`/workspace/${workspaceId}/dashboard/folders`);
        return response.data;
    } catch (error) {
        throw error;
    }
};

export const GetDashboardLists = async (workspaceId: string): Promise<ListItems> => {
    try {
        const response = await apiClient.get<ListItems>(`/workspace/${workspaceId}/dashboard/lists`);
        return response.data;
    } catch (error) {
        throw error;
    }
};

export const GetDashboardTasks = async (workspaceId: string): Promise<TaskItems> => {
    try {
        const response = await apiClient.get<TaskItems>(`/workspace/${workspaceId}/dashboard/tasks`);
        return response.data;
    } catch (error) {
        throw error;
    }
};