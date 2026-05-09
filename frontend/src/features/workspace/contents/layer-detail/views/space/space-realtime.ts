import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { signalRService } from "@/lib/signalr-service";

import { workspaceKeys } from "@/features/main/query-keys";
import { hierarchyKeys } from "../../../hierarchy/hierarchy-keys";

export function useSpaceRealtime(workspaceId: string) {
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

    const onSpaceUpdated = (data: any) => {
      const spaceData = normalizePayload(data.space || data);
      const id = spaceData.spaceId || spaceData.id;

      queryClient.setQueryData(
        [...workspaceKeys.all, "space", id],
        (old: any) => (old ? { ...old, ...spaceData } : spaceData),
      );

      // Invalidate the structure tree
      queryClient.invalidateQueries({
        queryKey: hierarchyKeys.detail(workspaceId),
        exact: true,
      });
    };

    const onHierarchyChanged = () => {
      queryClient.invalidateQueries({ queryKey: hierarchyKeys.all });
    };

    const onSpaceCreated = () => {
      queryClient.invalidateQueries({
        queryKey: hierarchyKeys.detail(workspaceId),
      });
    };

    const onSpaceDeleting = () => {
      queryClient.invalidateQueries({
        queryKey: hierarchyKeys.detail(workspaceId),
      });
    };

    signalRService.on("SpaceUpdated", onSpaceUpdated);
    signalRService.on("HierarchyChanged", onHierarchyChanged);
    signalRService.on("SpaceCreated", onSpaceCreated);
    signalRService.on("SpaceDeleting", onSpaceDeleting);

    return () => {
      signalRService.off("SpaceUpdated", onSpaceUpdated);
      signalRService.off("HierarchyChanged", onHierarchyChanged);
      signalRService.off("SpaceCreated", onSpaceCreated);
      signalRService.off("SpaceDeleting", onSpaceDeleting);
    };
  }, [workspaceId, queryClient]);
}
