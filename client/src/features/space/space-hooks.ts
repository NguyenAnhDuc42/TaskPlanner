import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createSpace, getSpaceTasks } from "./space-api";
import { WORKSPACE_KEYS } from "../workspace/workspace-hooks";
import { toast } from "sonner";
import { ErrorResponse } from "@/types/responses/error-response";
import { TaskList } from "./space-type";

export const SPACE_KEYS = {
    all: ["spaces"] as const,
    tasks: (spaceId: string) => [...SPACE_KEYS.all, spaceId, "tasks"] as const,
} as const;

export function useCreateSpace() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: createSpace,
        onSuccess: (data, variables) => {
            toast.success(data.message || "Space created successfully!");
            queryClient.invalidateQueries({
                queryKey: WORKSPACE_KEYS.hierarchy(variables.workspaceId)
            });
        },
        onError: (error: ErrorResponse) => {
            toast.error(error.detail || error.title || "Failed to create space.");
        }
    });
}

export function useSpaceTasks(spaceId: string | undefined) {
    return useQuery<TaskList, ErrorResponse>({
        queryKey: spaceId ? SPACE_KEYS.tasks(spaceId) : ["disabled-space-tasks-query"],
        queryFn: () => {
            if (!spaceId) throw new Error("Space ID is required to fetch tasks.");
            return getSpaceTasks({ spaceId });
        },
        enabled: !!spaceId,
    });
}