import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { CreateWorkspace, SidebarWorkspaces } from "./workspace-api";

import { ErrorResponse } from "@/types/responses/error-response";


export const WORKSPACE_KEYS = {
  all: ["workspaces"] as const,
  sidebar: () => [...WORKSPACE_KEYS.all, "sidebar"] as const,
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
        staleTime : 5 * 60 * 1000
    })
}
