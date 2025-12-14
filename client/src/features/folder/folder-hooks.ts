import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createFolder, getFolderTasks } from "./folder-api";
import { WORKSPACE_KEYS } from "../workspace/workspace-hooks";
import { toast } from "sonner";
import { ErrorResponse } from "@/types/responses/error-response";
import { TaskList } from "../space/space-type";

export const FOLDER_KEYS = {
    all: ["folders"] as const,
    tasks: (folderId: string) => [...FOLDER_KEYS.all, folderId, "tasks"] as const,
} as const;

export function useCreateFolder() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: createFolder,
        onSuccess: (data, variables) => {
            toast.success(data.message || "Folder created successfully!");
            queryClient.invalidateQueries({
                queryKey: WORKSPACE_KEYS.hierarchy(variables.workspaceId)
            });
        },
        onError: (error: ErrorResponse) => {
            toast.error(error.detail || error.title || "Failed to create folder.");
        }
    });
}
export function useFolderTasks(folderId: string | undefined) {
    return useQuery<TaskList, ErrorResponse>({
        queryKey: folderId ? FOLDER_KEYS.tasks(folderId) : ["disabled-folder-tasks-query"],
        queryFn: () => {
            if (!folderId) throw new Error("Folder ID is required to fetch tasks.");
            return getFolderTasks({ folderId });
        },
        enabled: !!folderId,
    });
}