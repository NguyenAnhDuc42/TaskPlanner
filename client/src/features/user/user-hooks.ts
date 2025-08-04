import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { getWorkspaces, joinWorkspace } from "./user-api";
import { JoinWorkspaceResponse } from "./user-type";
import { WORKSPACE_KEYS } from "../workspace/workspace-hooks";
import { WorkspaceDetail } from "@/types/workspace";

export const USER_KEYS  = {
    user : ["user"] as const,
    workspaces :() => [...USER_KEYS.user,"workspaces"] as const,
} as const 

export function useJoinWorkspace(){
    const queryClient = useQueryClient();
    const router = useRouter();

    return useMutation<JoinWorkspaceResponse, Error, string>({
        mutationFn: (joinCode: string) => joinWorkspace(joinCode),
        onSuccess: (data) => {
            toast.success(data.message);
            queryClient.invalidateQueries({ queryKey: WORKSPACE_KEYS.sidebar() });
            router.push(`/ws/${data.workspaceId}`);
        },
        onError: (error: Error) => {
            toast.error(error.message || "Failed to join workspace.");
        },
    });
}
export function useGetWorkspaces(){
    return useQuery<WorkspaceDetail[]>({
        queryKey: USER_KEYS.workspaces(),
        queryFn: async () => {
            return getWorkspaces();
        },
        staleTime: 5 * 60 * 1000,
    })
    
}