import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api-client";
import { workspaceKeys } from "../main/query-keys";
import { type WorkspaceTheme } from "./type";

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
};

export function useWorkspaceDetail(workspaceId: string) {
  return useQuery(workspaceQueryOptions.detail(workspaceId));
}
