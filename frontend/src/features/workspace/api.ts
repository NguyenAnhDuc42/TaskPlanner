import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api-client";
import { workspaceKeys } from "../main/query-keys";
import type { Theme } from "@/types/theme";

export interface WorkspaceSecurityContext {
  workspaceId: string;
  currentRole: string;
  isOwned: boolean;
  theme: Theme;
  color: string;
  icon: string;
}

export const workspaceQueryOptions = {
  detail: (workspaceId: string) => ({
    queryKey: [...workspaceKeys.all, "detail", workspaceId],
    queryFn: async () => {
      const { data } = await api.get<WorkspaceSecurityContext>(`/workspaces/${workspaceId}/me/permissions`);
      return data;
    },
    enabled: !!workspaceId,
    staleTime: 1000 * 60 * 5, // 5 minutes
  }),
  workflows: (workspaceId: string) => ({
    queryKey: [...workspaceKeys.all, "workflows", workspaceId],
    queryFn: async () => {
      const { data } = await api.get<any[]>(`/workflows`);
      return data;
    },
    enabled: !!workspaceId,
  }),
  spaceDetail: (workspaceId: string, spaceId: string, enabled = true) => ({
    queryKey: [...workspaceKeys.all, "space", spaceId],
    queryFn: async () => {
      const { data } = await api.get(`/spaces/${spaceId}`);
      return data;
    },
    enabled: !!workspaceId && !!spaceId && enabled,
  }),
  folderDetail: (workspaceId: string, folderId: string, enabled = true) => ({
    queryKey: [...workspaceKeys.all, "folder", folderId],
    queryFn: async () => {
      const { data } = await api.get(`/folders/${folderId}`);
      return data;
    },
    enabled: !!workspaceId && !!folderId && enabled,
  }),
  taskDetail: (workspaceId: string, taskId: string, enabled = true) => ({
    queryKey: [...workspaceKeys.all, "task", taskId],
    queryFn: async () => {
      const { data } = await api.get(`/tasks/${taskId}`);
      return data;
    },
    enabled: !!workspaceId && !!taskId && enabled,
  }),
  members: (workspaceId: string) => ({
    queryKey: [...workspaceKeys.all, "members", workspaceId],
    queryFn: async () => {
      const { data } = await api.get(`/workspaces/${workspaceId}/members?pageSize=1000`);
      return data;
    },
    enabled: !!workspaceId,
  }),
};

export function useWorkspaceDetail(workspaceId: string) {
  return useQuery(workspaceQueryOptions.detail(workspaceId));
}

export function useWorkspaceWorkflows(workspaceId: string) {
  return useQuery(workspaceQueryOptions.workflows(workspaceId));
}

export function useWorkspaceMembers(workspaceId: string) {
  return useQuery(workspaceQueryOptions.members(workspaceId));
}

export function useSpaceDetail(workspaceId: string, spaceId: string, enabled = true) {
  return useQuery(workspaceQueryOptions.spaceDetail(workspaceId, spaceId, enabled));
}

export function useFolderDetail(workspaceId: string, folderId: string, enabled = true) {
  return useQuery(workspaceQueryOptions.folderDetail(workspaceId, folderId, enabled));
}

export function useTaskDetail(workspaceId: string, taskId: string, enabled = true) {
  return useQuery(workspaceQueryOptions.taskDetail(workspaceId, taskId, enabled));
}
