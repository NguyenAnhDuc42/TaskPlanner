import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { CreateWorkspace, GetHierarchy, SidebarWorkspaces } from "./workspace-api";

import { ErrorResponse } from "@/types/responses/error-response";
import { Hierarchy } from "./workspacetype";


export const WORKSPACE_KEYS = {
  all: ["workspaces"] as const,
  sidebar: () => [...WORKSPACE_KEYS.all, "sidebar"] as const,
  hierarchy: (workspaceId: string) => [...WORKSPACE_KEYS.all, "hierarchy", workspaceId] as const,

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

export function useSidebarWorkspaces() {
    return useQuery({
        queryKey: WORKSPACE_KEYS.sidebar(),
        queryFn: SidebarWorkspaces,
    })
}

export function useHierarchy(workspaceId: string | undefined) {
  return useQuery<Hierarchy, ErrorResponse>({
    queryKey: workspaceId 
      ? WORKSPACE_KEYS.hierarchy(workspaceId) 
      : ["disabled-hierarchy-query"],
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
