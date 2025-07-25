import apiClient from "@/lib/api-client";
import { AddMembersBody, AddMembersResponse, CreateWorkspaceRequest, CreateWorkspaceResponse, GetHierarchyRequest, Hierarchy, Workspaces,  } from "./workspacetype";
import { Member, Members } from "@/types/user";
import { mapRoleFromApi } from "@/utils/role-utils";
import { FolderItems } from "@/types/folder";
import { ListItems } from "@/types/list";
import { TaskItems } from "@/types/task";
 
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
        const response = await apiClient.get<{ members: (Omit<Member, 'role'> & { role: number })[] }>(`/workspace/${workspaceId}/members`);
        const mappedMembers = response.data.members.map(member => ({
            ...member,
            role: mapRoleFromApi(member.role)
        }));

        return { members: mappedMembers };
    } catch (error) {
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