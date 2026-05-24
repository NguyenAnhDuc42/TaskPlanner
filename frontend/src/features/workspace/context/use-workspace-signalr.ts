import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { signalRService } from "@/lib/signalr-service";
import { hierarchyKeys } from "../contents/hierarchy/hierarchy-keys";
import { useAuth } from "@/features/auth/auth-context";

export function useWorkspaceSignalR(workspaceId: string) {
  const queryClient = useQueryClient();
  const { user } = useAuth();

  useEffect(() => {
    if (!workspaceId) return;

    const manageConnection = async () => {
      try {
        await signalRService.startConnection();
        await signalRService.invoke("JoinWorkspace", workspaceId);
      } catch (err) {
        console.error("[SignalR] Join error:", err);
      }
    };
    
    manageConnection();

    const onHierarchyChanged = (payload: any) => {
      // Ignore our own events to prevent Optimistic UI glitches
      if (user && payload.senderId === user.id) return;

      if (payload.itemType === "ProjectTask") {
        if (payload.targetParentId) {
          queryClient.invalidateQueries({ queryKey: hierarchyKeys.nodeTasks(workspaceId, payload.targetParentId) });
        }
        if (payload.sourceParentId) {
          queryClient.invalidateQueries({ queryKey: hierarchyKeys.nodeTasks(workspaceId, payload.sourceParentId) });
        }
      } else if (payload.itemType === "ProjectFolder") {
        if (payload.targetParentId) {
          queryClient.invalidateQueries({ queryKey: hierarchyKeys.nodeFolders(workspaceId, payload.targetParentId) });
        }
        if (payload.sourceParentId) {
          queryClient.invalidateQueries({ queryKey: hierarchyKeys.nodeFolders(workspaceId, payload.sourceParentId) });
        }
      } else if (payload.itemType === "ProjectSpace") {
        queryClient.invalidateQueries({ queryKey: hierarchyKeys.detail(workspaceId) });
      }
    };

    signalRService.on("HierarchyChanged", onHierarchyChanged);

    return () => {
      signalRService.off("HierarchyChanged", onHierarchyChanged);
      signalRService.invoke("LeaveWorkspace", workspaceId).catch(() => {});
    };
  }, [workspaceId, queryClient, user?.id]);
}
