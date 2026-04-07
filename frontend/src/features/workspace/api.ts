import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api-client";
import { workspaceKeys } from "../main/query-keys";
import { type WorkspaceTheme } from "./type";
import type { StatusDto } from "./contents/hierarchy/views/views-type";

export interface WorkspaceSecurityContext {
  workspaceId: string;
  currentRole: string;
  isOwned: boolean;
  theme: WorkspaceTheme;
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
  statuses: () => ({
    queryKey: [...workspaceKeys.all, "statuses"],
    queryFn: async () => {
      const { data } = await api.get<StatusDto[]>("/workspaces/statuses");
      return data;
    },
    staleTime: 1000 * 60 * 60, // 1 hour
  }),
};

export function useWorkspaceDetail(workspaceId: string) {
  return useQuery(workspaceQueryOptions.detail(workspaceId));
}

export function useWorkspaceStatuses() {
  return useQuery(workspaceQueryOptions.statuses());
}
