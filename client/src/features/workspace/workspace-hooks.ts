import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  AddMembers,
  CreateWorkspace,
  GetDashboardFolders,
  GetDashboardLists,
  GetDashboardTasks,
  GetHierarchy,
  GetMembers,
  SidebarWorkspaces,
} from "./workspace-api";

import { ErrorResponse } from "@/types/responses/error-response";
import { AddMembersBody, Hierarchy } from "./workspacetype";
import { FolderItems } from "@/types/folder";
import { ListItems } from "@/types/list";
import { TaskItems } from "@/types/task";

export const WORKSPACE_KEYS = {
  all: ["workspaces"] as const,
  sidebar: () => [...WORKSPACE_KEYS.all, "sidebar"] as const,
  hierarchy: (workspaceId: string) => [...WORKSPACE_KEYS.all, "hierarchy", workspaceId] as const,
  members: (workspaceId: string) => [...WORKSPACE_KEYS.all, "members", workspaceId] as const,
  dashboard: (workspaceId: string) => [...WORKSPACE_KEYS.all, "dashboard", workspaceId] as const,
  dashboardFolders: (workspaceId: string) => [...WORKSPACE_KEYS.dashboard(workspaceId), "folders"] as const,
  dashboardLists: (workspaceId: string) => [...WORKSPACE_KEYS.dashboard(workspaceId), "lists"] as const,
  dashboardTasks: (workspaceId: string) => [...WORKSPACE_KEYS.dashboard(workspaceId), "tasks"] as const,
} as const

export function useCreateWorkspace() {
    const queryClient = useQueryClient();
  return useMutation({
    mutationFn: CreateWorkspace,
    onSuccess: () => {
        queryClient.invalidateQueries({
            queryKey: WORKSPACE_KEYS.sidebar()
        })
    },
    onError: (error: ErrorResponse) => {
      console.error("Register Mutation Error:", error);
      console.log(error);
    },
  });
}

export function useGetDashboardFolders(workspaceId: string | undefined) {
  return useQuery<FolderItems, ErrorResponse>({
    queryKey: workspaceId ? WORKSPACE_KEYS.dashboardFolders(workspaceId) : ["disabled-dashboard-folders-query"],
    queryFn: async () => {
      if (!workspaceId) {
        throw new Error("Workspace ID is required");
      }
      return GetDashboardFolders(workspaceId);
    },
    enabled: !!workspaceId,
    staleTime: 5 * 60 * 1000,
  });
}

export function useGetDashboardLists(workspaceId: string | undefined) {
  return useQuery<ListItems, ErrorResponse>({
    queryKey: workspaceId ? WORKSPACE_KEYS.dashboardLists(workspaceId) : ["disabled-dashboard-lists-query"],
    queryFn: async () => {
      if (!workspaceId) {
        throw new Error("Workspace ID is required");
      }
      return GetDashboardLists(workspaceId);
    },
    enabled: !!workspaceId,
    staleTime: 5 * 60 * 1000,
  });
}

export function useGetDashboardTasks(workspaceId: string | undefined) {
  return useQuery<TaskItems, ErrorResponse>({
    queryKey: workspaceId ? WORKSPACE_KEYS.dashboardTasks(workspaceId) : ["disabled-dashboard-tasks-query"],
    queryFn: async () => {
      if (!workspaceId) {
        throw new Error("Workspace ID is required");
      }
      return GetDashboardTasks(workspaceId);
    },
    enabled: !!workspaceId,
    staleTime: 5 * 60 * 1000,
  });
}

export function useSidebarWorkspaces() {
    return useQuery({
        queryKey: WORKSPACE_KEYS.sidebar(),
        queryFn: SidebarWorkspaces,
    })
}

export function useHierarchy(workspaceId: string | undefined) {
  return useQuery<Hierarchy, ErrorResponse>({
    queryKey: workspaceId ? WORKSPACE_KEYS.hierarchy(workspaceId) : ["disabled-hierarchy-query"],
    queryFn: async () => {
      if (!workspaceId) {
        throw new Error('Workspace ID is required');
      }
      return GetHierarchy({ id: workspaceId });
    },
    enabled: !!workspaceId,
    staleTime: 5 * 60 * 1000, 
  });
}

export function useGetMembers(workspaceId: string | undefined){
  return useQuery({
    queryKey: workspaceId ? WORKSPACE_KEYS.members(workspaceId) : ["disabled-members-query"],
    queryFn: async () => {
      if (!workspaceId) {
        throw new Error('Workspace ID is required');
      }
      return GetMembers(workspaceId);
    },
    enabled: !!workspaceId,
    staleTime: 5 * 60 * 1000, 
  });
}
export function useAddMembers(workspaceId: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (datas: AddMembersBody) => {
      if (!workspaceId) {
        throw new Error('Workspace ID is required');
      }
      return AddMembers(workspaceId, datas);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: WORKSPACE_KEYS.members(workspaceId || ""),
      });
    },
    onError: (error: ErrorResponse) => {
      console.error("Add Members Mutation Error:", error);
    },
  });
}
