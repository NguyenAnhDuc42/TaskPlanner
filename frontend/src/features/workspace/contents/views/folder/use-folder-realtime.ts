import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { signalRService } from "@/lib/signalr-service";
import { folderQueryOptions } from "./folder-api";

export function useFolderRealtime(folderId: string) {
  const queryClient = useQueryClient();

  useEffect(() => {
    if (!folderId) return;

    const onTaskUpdated = (data: any) => {
      const taskData = data.task || data;
      // If the updated task belongs to this folder, invalidate the tasks list
      if (taskData.folderId === folderId || taskData.FolderId === folderId) {
        queryClient.invalidateQueries({
          queryKey: ["folderTasks", folderId]
        });
      }
    };

    const onBatchUpdated = (data: any) => {
      if (data.folderId === folderId || data.FolderId === folderId) {
        queryClient.invalidateQueries({
          queryKey: ["folderTasks", folderId]
        });
      }
    };

    signalRService.on("TaskUpdated", onTaskUpdated);
    signalRService.on("FolderTasksBatchUpdated", onBatchUpdated);

    return () => {
      signalRService.off("TaskUpdated", onTaskUpdated);
      signalRService.off("FolderTasksBatchUpdated", onBatchUpdated);
    };
  }, [folderId, queryClient]);
}
