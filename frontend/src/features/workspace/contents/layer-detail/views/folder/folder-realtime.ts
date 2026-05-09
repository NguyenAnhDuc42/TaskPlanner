import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { signalRService } from "@/lib/signalr-service";
import { workspaceKeys } from "@/features/main/query-keys";
import { hierarchyKeys } from "../../../hierarchy/hierarchy-keys";

export function useFolderRealtime(workspaceId: string) {
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

    const onFolderUpdated = (data: any) => {
      const folderData = normalizePayload(data.folder || data);
      const id = folderData.folderId || folderData.id;
      const spaceId = folderData.spaceId;

      queryClient.setQueryData(
        [...workspaceKeys.all, "folder", id],
        (old: any) => (old ? { ...old, ...folderData } : folderData),
      );

      // Invalidate folders list under the parent space
      if (spaceId) {
        queryClient.invalidateQueries({
          queryKey: hierarchyKeys.nodeFolders(workspaceId, spaceId),
          exact: true,
        });
      }
    };

    const onFolderCreated = (data: any) => {
      const spaceId = data.spaceId || data.SpaceId;
      if (spaceId) {
        queryClient.invalidateQueries({
          queryKey: hierarchyKeys.nodeFolders(workspaceId, spaceId),
          exact: true,
        });
      }
      // Also invalidate the main hierarchy tree
      queryClient.invalidateQueries({
        queryKey: hierarchyKeys.detail(workspaceId),
      });
    };

    const onFolderDeleting = (data: any) => {
      const spaceId = data.spaceId || data.SpaceId;
      if (spaceId) {
        queryClient.invalidateQueries({
          queryKey: hierarchyKeys.nodeFolders(workspaceId, spaceId),
          exact: true,
        });
      }
      // Also invalidate the main hierarchy tree
      queryClient.invalidateQueries({
        queryKey: hierarchyKeys.detail(workspaceId),
      });
    };

    signalRService.on("FolderUpdated", onFolderUpdated);
    signalRService.on("FolderCreated", onFolderCreated);
    signalRService.on("FolderDeleting", onFolderDeleting);

    return () => {
      signalRService.off("FolderUpdated", onFolderUpdated);
      signalRService.off("FolderCreated", onFolderCreated);
      signalRService.off("FolderDeleting", onFolderDeleting);
    };
  }, [workspaceId, queryClient]);
}
