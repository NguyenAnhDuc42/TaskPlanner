import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api-client";
import type { WorkspaceHierarchy } from "./hierarchy-type";
import { hierarchyKeys } from "./hierarchy-keys";

export function useHierarchy(workspaceId: string) {
  return useQuery({
    queryKey: hierarchyKeys.detail(workspaceId),
    queryFn: async () => {
      const { data } = await api.get<WorkspaceHierarchy>(
        `/workspaces/${workspaceId}/hierarchy`,
      );
      return data;
    },
    staleTime: 1000 * 60 * 5, // 5 minutes
  });
}
