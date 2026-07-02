import { useEffect, useRef, useLayoutEffect } from "react";
import { useDispatch } from "react-redux";
import { useUser } from "@/features/auth/auth-api";
import { store } from "@/store";
import { signalRService } from "@/lib/signalr-service";
import {
  spaceSlice,
  folderSlice,
  taskSlice,
  memberSlice,
  statusSlice,
  assigneeSlice,
  commentSlice,
  workspaceSlice,
  attachmentSlice,
  documentBlockSlice,
  memberSelectors,
  taskSelectors,
} from "@/store/entityStore";
import { workspaceApi } from "@/store/workspaceApi";
import type { AppDispatch } from "@/store";

export function useWorkspaceSignalR(workspaceId: string) {
  const dispatch = useDispatch<AppDispatch>();
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
      console.log("[SignalR] EntitiesUpdated:", payload);
      const user = currentUserRef.current;
      if (payload.spaces) {
        dispatch(spaceSlice.actions.upsertMany(payload.spaces));
      }
      if (payload.folders) {
        dispatch(folderSlice.actions.upsertMany(payload.folders));
        payload.folders.forEach(f => {
          if (f.spaceId) dispatch(spaceSlice.actions.upsert({ id: f.spaceId, hasFolders: true }));
        });
      }
      if (payload.tasks) {
        dispatch(taskSlice.actions.upsertMany(payload.tasks));
        payload.tasks.forEach(t => {
          if (t.folderId) {
            dispatch(folderSlice.actions.upsert({ id: t.folderId, hasTasks: true }));
          } else if (t.spaceId) {
            dispatch(spaceSlice.actions.upsert({ id: t.spaceId, hasTasks: true }));
          }
        });
      }
      if (payload.members) {
        dispatch(memberSlice.actions.upsertMany(payload.members));
        if (user?.id && payload.members.some(m => m.userId === user.id)) {
          dispatch(workspaceApi.util.invalidateTags([{ type: "Workspaces", id: workspaceId }]));
        }
      }
      if (payload.assignees)      dispatch(assigneeSlice.actions.upsertMany(payload.assignees));
      if (payload.statuses)       dispatch(statusSlice.actions.upsertMany(payload.statuses));
      if (payload.comments)       dispatch(commentSlice.actions.upsertMany(payload.comments));
      if (payload.workspaces)     dispatch(workspaceSlice.actions.upsertMany(payload.workspaces));
      if (payload.attachments)    dispatch(attachmentSlice.actions.upsertMany(payload.attachments));
      if (payload.documentBlocks) {
        dispatch(documentBlockSlice.actions.upsertMany(payload.documentBlocks));
        // Invalidate RTK cache so the open editor refetches fresh blocks
        const docIds = [...new Set(payload.documentBlocks.map(b => b.documentId).filter(Boolean))];
        docIds.forEach(docId => {
          dispatch(workspaceApi.util.invalidateTags([{ type: "Documents" as const, id: `blocks-${docId}` }]));
        });
      }
    };

    const onEntitiesDeleted = (payload: import("@/lib/signalr-service").EntityBatchDelete) => {
      console.log("[SignalR] EntitiesDeleted:", payload);
      const user = currentUserRef.current;
      if (payload.spaceIds)          dispatch(spaceSlice.actions.removeMany(payload.spaceIds));
      if (payload.folderIds) {
        const state = store.getState();

        // Move tasks in deleted folders to space level so they stay visible
        const orphaned = taskSelectors.selectAll(state)
          .filter(t => t.folderId && payload.folderIds!.includes(t.folderId))
          .map(t => ({ id: t.id, folderId: null }));
        if (orphaned.length > 0) dispatch(taskSlice.actions.upsertMany(orphaned));

        dispatch(folderSlice.actions.removeMany(payload.folderIds));
      }
      if (payload.taskIds)           dispatch(taskSlice.actions.removeMany(payload.taskIds));
      if (payload.memberIds) {
        const currentMembers = memberSelectors.selectEntities(store.getState());
        const isCurrentUserDeleted = payload.memberIds.some(id => {
          const m = currentMembers[id];
          return m && m.userId === user?.id;
        });
        dispatch(memberSlice.actions.removeMany(payload.memberIds));
        if (isCurrentUserDeleted) {
          dispatch(workspaceApi.util.invalidateTags([{ type: "Workspaces", id: workspaceId }]));
        }
      }
      if (payload.assigneeIds)       dispatch(assigneeSlice.actions.removeMany(payload.assigneeIds));
      if (payload.statusIds)         dispatch(statusSlice.actions.removeMany(payload.statusIds));
      if (payload.commentIds)        dispatch(commentSlice.actions.removeMany(payload.commentIds));
      if (payload.workspaceIds)      dispatch(workspaceSlice.actions.removeMany(payload.workspaceIds));
      if (payload.attachmentIds)     dispatch(attachmentSlice.actions.removeMany(payload.attachmentIds));
      if (payload.documentBlockIds)  dispatch(documentBlockSlice.actions.removeMany(payload.documentBlockIds));
    };

    const handleReconnect = () => {
      console.log("[SignalR] Reconnected. Syncing active screen views...");
      signalRService.invoke("JoinWorkspace", workspaceId).catch(() => {});
      const userId = currentUserRef.current?.id;
      if (userId) signalRService.invoke("JoinUser", userId).catch(() => {});
      dispatch(workspaceApi.util.invalidateTags(['Workspaces', 'Spaces', 'Folders', 'Tasks', 'Members', 'User', 'UserPreference', 'EntityAccess', 'Workflows', 'Comments', 'Documents']));
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
