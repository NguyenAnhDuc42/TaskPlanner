import { useEffect, useRef, useLayoutEffect } from "react";
import { useDispatch } from "react-redux";
import { useUser } from "@/features/auth/auth-api";
import { useStore } from "@/stores/root.store";
import { signalRService } from "@/lib/signalr-service";
import { workspaceApi } from "@/store/workspaceApi";
import { workspaceSlice } from "@/store/entityStore";
import type { AppDispatch } from "@/store";

// Space/Folder/Task/Status/Assignee/Member data now flows through the new SyncHub Delta/DeltaBatch
// path into MobX (see sync-engine.ts) — this hook used to also dispatch that same data into the
// legacy Redux entityStore, but nothing reads those slices anymore (confirmed: no component
// subscribes to spaceSelectors/folderSelectors/taskSelectors/assigneeSelectors/memberSelectors/
// statusSelectors/commentSelectors/attachmentSelectors/documentBlockSelectors outside this file
// and entityStore.ts itself). What's left here is the one still-load-bearing side effect: telling
// RTK Query to refetch the current user's own workspace permissions when their membership changes.
export function useWorkspaceSignalR(workspaceId: string) {
  const dispatch = useDispatch<AppDispatch>();
  const rootStore = useStore();
  const { data: currentUser } = useUser();

  // Ref so handlers always read a fresh value without re-registering on every render
  const currentUserRef = useRef(currentUser);
  useLayoutEffect(() => { currentUserRef.current = currentUser; });

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
      const user = currentUserRef.current;
      // Space/Folder/Task/Status/Assignee/Comment/Attachment/DocumentBlock updates are handled by
      // the new SyncHub Delta path now — this channel only still matters for (a) the current
      // user's own membership changing (forces a workspace-permissions refetch) and (b) the
      // top-level Workspace record itself (home-screen list/switcher still read that via Redux).
      if (payload.members && user?.id && payload.members.some(m => m.userId === user.id)) {
        dispatch(workspaceApi.util.invalidateTags([{ type: "Workspaces", id: workspaceId }]));
      }
      if (payload.workspaces) dispatch(workspaceSlice.actions.upsertMany(payload.workspaces));
    };

    const onEntitiesDeleted = (payload: import("@/lib/signalr-service").EntityBatchDelete) => {
      const user = currentUserRef.current;
      if (payload.memberIds) {
        const isCurrentUserDeleted = payload.memberIds.some(
          (id) => rootStore.memberStore.getById(id)?.userId === user?.id,
        );
        if (isCurrentUserDeleted) {
          dispatch(workspaceApi.util.invalidateTags([{ type: "Workspaces", id: workspaceId }]));
        }
      }
      if (payload.workspaceIds) dispatch(workspaceSlice.actions.removeMany(payload.workspaceIds));
    };

    const handleReconnect = () => {
      console.log("[SignalR] Reconnected. Syncing active screen views...");
      signalRService.invoke("JoinWorkspace", workspaceId).catch(() => {});
      const userId = currentUserRef.current?.id;
      if (userId) signalRService.invoke("JoinUser", userId).catch(() => {});
      // Only tags still actually provided by a live RTK Query endpoint — the rest (Spaces,
      // Folders, Tasks, Members, EntityAccess, Workflows, Comments, Documents) belonged to
      // endpoints now superseded by the sync engine and removed from features/workspace/api.ts.
      dispatch(workspaceApi.util.invalidateTags(['Workspaces', 'User', 'UserPreference']));
    };

    signalRService.on("EntitiesUpdated", onEntitiesUpdated);
    signalRService.on("EntitiesDeleted", onEntitiesDeleted);
    signalRService.onReconnected(handleReconnect);
    const unregisterVisibility = signalRService.registerVisibilityReconnect();

    return () => {
      signalRService.off("EntitiesUpdated", onEntitiesUpdated);
      signalRService.off("EntitiesDeleted", onEntitiesDeleted);
      signalRService.offReconnected(handleReconnect);
      unregisterVisibility();
      signalRService.invoke("LeaveWorkspace", workspaceId).catch(() => {});
    };
  }, [workspaceId, dispatch]);
}
