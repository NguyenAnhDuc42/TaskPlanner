import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { signalRService } from "@/lib/signalr-service";
import { hierarchyKeys } from "../../hierarchy/hierarchy-keys";
import { workspaceKeys } from "@/features/main/query-keys";

export function useLayerRealtime(workspaceId: string) {
  const queryClient = useQueryClient();

  useEffect(() => {
    console.log(`📡 useLayerRealtime Hook Init for Workspace: ${workspaceId}`);
    if (!workspaceId) return;

    // --- Normalization Helper ---
    const normalizePayload = (raw: any) => {
      const normalized: any = {};

      // Mapping PascalCase (SignalR) to camelCase (Frontend)
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
        OrderKey: "orderKey",
      };

      // Apply mappings
      Object.keys(raw).forEach((key) => {
        const normalizedKey =
          mappings[key] || key.charAt(0).toLowerCase() + key.slice(1);
        if (raw[key] !== undefined) {
          normalized[normalizedKey] = raw[key];
        }
      });

      return normalized;
    };

    // --- Smart Cache Invalidator ---
    const invalidateHierarchy = (
      type: "space" | "folder" | "task",
      parentId?: string,
    ) => {
      // 1. Only invalidate the structure tree for SPACE updates
      if (type === "space") {
        queryClient.invalidateQueries({
          queryKey: [...hierarchyKeys.all, workspaceId, "structure"],
          exact: true,
        });
      }

      // 2. Target specific node lists for FOLDERS (Space is the parent)
      if (type === "folder" && parentId) {
        queryClient.invalidateQueries({
          queryKey: [
            ...hierarchyKeys.all,
            workspaceId,
            "node",
            parentId,
            "folders",
          ],
          exact: true,
        });
      }

      // 3. Target specific node lists for TASKS (Space or Folder is the parent)
      if (type === "task" && parentId) {
        queryClient.invalidateQueries({
          queryKey: [
            ...hierarchyKeys.all,
            workspaceId,
            "node",
            parentId,
            "tasks",
          ],
          exact: true,
        });
      }
    };

    const onSpaceUpdated = (data: any) => {
      const spaceData = normalizePayload(data.space || data);
      const id = spaceData.spaceId || spaceData.id;

      queryClient.setQueryData(
        [...workspaceKeys.all, "space", id],
        (old: any) => (old ? { ...old, ...spaceData } : spaceData),
      );
      invalidateHierarchy("space");
    };

    const onFolderUpdated = (data: any) => {
      const folderData = normalizePayload(data.folder || data);
      const id = folderData.folderId || folderData.id;
      const spaceId = folderData.spaceId;

      queryClient.setQueryData(
        [...workspaceKeys.all, "folder", id],
        (old: any) => (old ? { ...old, ...folderData } : folderData),
      );
      invalidateHierarchy("folder", spaceId);
    };

    const onTaskUpdated = (data: any) => {
      const taskData = normalizePayload(data.task || data);
      const id = taskData.taskId || taskData.id;
      const parentId = taskData.folderId || taskData.spaceId;

      queryClient.setQueryData(
        [...workspaceKeys.all, "task", id],
        (old: any) => (old ? { ...old, ...taskData } : taskData),
      );
      invalidateHierarchy("task", parentId);
    };

    const onHierarchyChanged = () => {
      queryClient.invalidateQueries({ queryKey: hierarchyKeys.all });
    };

    // Register Listeners (Adding both FolderUpdated and ProjectFolderUpdated for safety)
    signalRService.on("SpaceUpdated", onSpaceUpdated);
    signalRService.on("FolderUpdated", onFolderUpdated);
    signalRService.on("ProjectFolderUpdated", onFolderUpdated);
    signalRService.on("TaskUpdated", onTaskUpdated);
    signalRService.on("ProjectTaskUpdated", onTaskUpdated);
    signalRService.on("HierarchyChanged", onHierarchyChanged);

    return () => {
      signalRService.off("SpaceUpdated", onSpaceUpdated);
      signalRService.off("FolderUpdated", onFolderUpdated);
      signalRService.off("ProjectFolderUpdated", onFolderUpdated);
      signalRService.off("TaskUpdated", onTaskUpdated);
      signalRService.off("ProjectTaskUpdated", onTaskUpdated);
      signalRService.off("HierarchyChanged", onHierarchyChanged);
    };
  }, [workspaceId, queryClient]);
}
