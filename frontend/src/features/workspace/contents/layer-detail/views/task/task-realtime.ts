import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { signalRService } from "@/lib/signalr-service";
import { workspaceKeys } from "@/features/main/query-keys";
import { hierarchyKeys } from "../../../hierarchy/hierarchy-keys";

export function useTaskRealtime(workspaceId: string) {
  const queryClient = useQueryClient();

  useEffect(() => {
    if (!workspaceId) return;

    const normalizePayload = (raw: any) => {
      const normalized: any = {};
      const mappings: Record<string, string> = {
        Name: "name",
        Icon: "icon",
        Color: "color",
        Description: "description",
        IsPrivate: "isPrivate",
        StatusId: "statusId",
        Priority: "priority",
        StartDate: "startDate",
        DueDate: "dueDate",
      };

      Object.keys(raw).forEach((key) => {
        const normalizedKey =
          mappings[key] || key.charAt(0).toLowerCase() + key.slice(1);
        if (raw[key] !== undefined) {
          normalized[normalizedKey] = raw[key];
        }
      });

      return normalized;
    };

    const onTaskUpdated = (data: any) => {
      const taskData = normalizePayload(data.task || data);
      const id = taskData.taskId || taskData.id;
      const parentId = taskData.folderId || taskData.spaceId;

      queryClient.setQueryData(
        [...workspaceKeys.all, "task", id],
        (old: any) => (old ? { ...old, ...taskData } : taskData),
      );

      // Invalidate tasks list under the parent folder or space
      if (parentId) {
        queryClient.invalidateQueries({
          queryKey: hierarchyKeys.nodeTasks(workspaceId, parentId),
          exact: true,
        });
      }
    };

    signalRService.on("TaskUpdated", onTaskUpdated);

    return () => {
      signalRService.off("TaskUpdated", onTaskUpdated);
    };
  }, [workspaceId, queryClient]);
}
