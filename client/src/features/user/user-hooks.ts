import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { createWorkspace, getWorkspaces, joinWorkspace } from "./user-api";
import { JoinWorkspaceResponse } from "./user-type";
import { WORKSPACE_KEYS } from "../workspace/workspace-hooks";
import { WorkspaceDetail } from "@/types/workspace";
import { CreateWorkspaceRequest } from "../workspace/workspacetype";
import { ErrorResponse } from "@/types/responses/error-response";

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
            queryClient.invalidateQueries({ queryKey: USER_KEYS.workspaces() });
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

export function useCreateWorkspace() {
    const queryClient = useQueryClient();
    const router = useRouter();


    return useMutation<string, ErrorResponse, CreateWorkspaceRequest>({
        mutationFn: (data: CreateWorkspaceRequest) => createWorkspace(data),
        onSuccess: (data) => {
            toast.success("Workspace created successfully!");
            queryClient.invalidateQueries({ queryKey: USER_KEYS.workspaces() });
            router.push(`/ws/${data}`)  
        },
        onError: (error: ErrorResponse) => {
            toast.error(error.title || "Failed to create workspace.");
        },
    });
}