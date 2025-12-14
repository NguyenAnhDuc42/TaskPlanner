import { useMutation, useQueryClient } from "@tanstack/react-query";

import { WORKSPACE_KEYS } from "../workspace/workspace-hooks";
import { toast } from "sonner";
import { CreateSpace } from "./space-api";
import { CreateSpaceBody } from "./space-type";
import { ProblemDetails } from "@/types/responses/problem-details";


export const SPACE_KEYS = {
    all: ["spaces"] as const,
} as const;

export function useCreateSpace() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: ({ workspaceId, body }: { workspaceId: string; body: CreateSpaceBody }) => CreateSpace(workspaceId, body),
        onSuccess: (data, variables) => {
            toast.success(`Space created successfully with ID: ${data}`); 
            queryClient.invalidateQueries({
                queryKey: WORKSPACE_KEYS.hierarchy(variables.workspaceId)
            });
            // TODO: Consider direct cache update for immediate UI feedback or navigation to the new space
        },  
        onError: (error: ProblemDetails) => {
            toast.error(error.detail || error.title || "Failed to create space.");
        }
    });
}