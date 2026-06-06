import { useEffect } from "react";
import { useDispatch } from "react-redux";
import { signalRService } from "@/lib/signalr-service";
import { spaceSlice, folderSlice, taskSlice, memberSlice, assigneeSlice, entityAccessSlice } from "@/store/entityStore";
import { workspaceApi } from "@/store/workspaceApi";
import type { AppDispatch } from "@/store";

export function useWorkspaceSignalR(workspaceId: string) {
  const dispatch = useDispatch<AppDispatch>();

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

    const onEntitiesUpdated = (payload: import("@/lib/signalr-service").EntityBatchUpdate) => {
      if (payload.spaces)       dispatch(spaceSlice.actions.upsertMany(payload.spaces));
      if (payload.folders)      dispatch(folderSlice.actions.upsertMany(payload.folders));
      if (payload.tasks)        dispatch(taskSlice.actions.upsertMany(payload.tasks));
      if (payload.members)      dispatch(memberSlice.actions.upsertMany(payload.members));
      if (payload.assignees)    dispatch(assigneeSlice.actions.upsertMany(payload.assignees));
      if (payload.entityAccess) dispatch(entityAccessSlice.actions.upsertMany(payload.entityAccess));
    };

    const onEntitiesDeleted = (payload: import("@/lib/signalr-service").EntityBatchDelete) => {
      if (payload.spaceIds)        dispatch(spaceSlice.actions.removeMany(payload.spaceIds));
      if (payload.folderIds)       dispatch(folderSlice.actions.removeMany(payload.folderIds));
      if (payload.taskIds)         dispatch(taskSlice.actions.removeMany(payload.taskIds));
      if (payload.memberIds)       dispatch(memberSlice.actions.removeMany(payload.memberIds));
      if (payload.assigneeIds)     dispatch(assigneeSlice.actions.removeMany(payload.assigneeIds));
      if (payload.entityAccessIds) dispatch(entityAccessSlice.actions.removeMany(payload.entityAccessIds));
    };

    const handleReconnect = () => {
      console.log("[SignalR] Reconnected. Syncing active screen views...");
      dispatch(workspaceApi.util.invalidateTags(['Spaces', 'Folders', 'Tasks', 'Members']));
    };

    signalRService.on("EntitiesUpdated", onEntitiesUpdated);
    signalRService.on("EntitiesDeleted", onEntitiesDeleted);
    signalRService.onReconnected(handleReconnect);

    return () => {
      signalRService.off("EntitiesUpdated", onEntitiesUpdated);
      signalRService.off("EntitiesDeleted", onEntitiesDeleted);
      signalRService.offReconnected(handleReconnect);
      signalRService.invoke("LeaveWorkspace", workspaceId).catch(() => {});
    };
  }, [workspaceId, dispatch]);
}
