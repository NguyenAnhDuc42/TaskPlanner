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
      const { data } = await api.get<WorkspaceSecurityContext>(
        `/workspaces/${workspaceId}/me/permissions`,
      );
      return data;
    },
    enabled: !!workspaceId,
    staleTime: 1000 * 60 * 5, // 5 minutes
  }),
  workflows: (workspaceId: string, layerId?: string, layerType?: string) => ({
    queryKey: [...workspaceKeys.all, "workflows", workspaceId, layerId, layerType],
    queryFn: async () => {
      const { data } = await api.get<any[]>("/workflows", {
        params: { layerId, layerType },
      });
      return data;
    },
    enabled: !!workspaceId,
    staleTime: 0, // Always refetch to keep multi-user in sync
  }),
  members: (workspaceId: string) => ({
    queryKey: [...workspaceKeys.all, "members", workspaceId],
    queryFn: async () => {
      const { data } = await api.get(
        `/workspaces/${workspaceId}/members?pageSize=1000`,
      );
      return data;
    },
    enabled: !!workspaceId,
    staleTime: 1000 * 60 * 5,
  }),
};

export function useWorkspaceDetail(workspaceId: string) {
  return useQuery(workspaceQueryOptions.detail(workspaceId));
}

export function useWorkspaceWorkflows(workspaceId: string, layerId?: string, layerType?: string) {
  return useQuery(workspaceQueryOptions.workflows(workspaceId, layerId, layerType));
}

export function useWorkspaceMembers(workspaceId: string) {
  return useQuery(workspaceQueryOptions.members(workspaceId));
}
