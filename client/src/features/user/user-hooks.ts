import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { joinWorkspace } from "./user-api";
import { JoinWorkspaceResponse } from "./user-type";
import { WORKSPACE_KEYS } from "../workspace/workspace-hooks";

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