import { useEffect, useRef, useLayoutEffect } from "react";
import { useDispatch } from "react-redux";
import { useUser } from "@/features/auth/auth-api";
import { useNavigate, useLocation } from "@tanstack/react-router";
import { store } from "@/store";
import { signalRService } from "@/lib/signalr-service";
import {
  spaceSlice,
  folderSlice,
  taskSlice,
  memberSlice,
  statusSlice,
  assigneeSlice,
  entityAccessSlice,
  commentSlice,
  workspaceSlice,
  attachmentSlice,
  documentBlockSlice,
  memberSelectors,
  entityAccessSelectors,
  taskSelectors,
} from "@/store/entityStore";
import { workspaceApi } from "@/store/workspaceApi";
import { hierarchyApi } from "@/features/workspace/contents/hierarchy/hierarchy-api";
import type { AppDispatch } from "@/store";

export function useWorkspaceSignalR(workspaceId: string) {
  const dispatch = useDispatch<AppDispatch>();
  const { data: currentUser } = useUser();
  const navigate = useNavigate();
  const location = useLocation();

  // Refs so handlers always read fresh values without re-registering on every render
  const currentUserRef = useRef(currentUser);
  const navigateRef = useRef(navigate);
  const locationRef = useRef(location);
  useLayoutEffect(() => { currentUserRef.current = currentUser; });
  useLayoutEffect(() => { navigateRef.current = navigate; });
  useLayoutEffect(() => { locationRef.current = location; });

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
        const hasPrivacyChange = payload.spaces.some(s => s.isPrivate !== undefined);
        if (hasPrivacyChange) {
          dispatch(hierarchyApi.endpoints.getNodeSpaces.initiate(
            { workspaceId, cursor: null },
            { subscribe: false, forceRefetch: true }
          ));
        }
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
      if (payload.entityAccess) {
        dispatch(entityAccessSlice.actions.upsertMany(payload.entityAccess));
        const currentMemberId = store.getState().members.ids.find(
          id => store.getState().members.entities[id]?.userId === user?.id
        );
        const affectsCurrentUser = payload.entityAccess.some(
          ea => ea.workspaceMemberId === currentMemberId
        );
        if (affectsCurrentUser) {
          dispatch(hierarchyApi.endpoints.getNodeSpaces.initiate(
            { workspaceId, cursor: null },
            { subscribe: false, forceRefetch: true }
          ));
        }
      }
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
        // Collect affected space IDs before removing folders from store
        const affectedSpaceIds = payload.folderIds
          .map(id => state.folders.entities[id]?.spaceId)
          .filter((id): id is string => !!id);

        // Move tasks in deleted folders to space level so they stay visible
        const orphaned = taskSelectors.selectAll(state)
          .filter(t => t.folderId && payload.folderIds!.includes(t.folderId))
          .map(t => ({ id: t.id, folderId: null }));
        if (orphaned.length > 0) dispatch(taskSlice.actions.upsertMany(orphaned));

        dispatch(folderSlice.actions.removeMany(payload.folderIds));

        // Refetch space-level task lists so orphaned tasks appear in the sidebar
        affectedSpaceIds.forEach(spaceId => {
          dispatch(hierarchyApi.endpoints.getNodeTasks.initiate(
            { workspaceId, nodeId: spaceId, parentType: "ProjectSpace", cursor: null },
            { subscribe: false, forceRefetch: true }
          ));
        });
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
      if (payload.entityAccessIds) {
        const state = store.getState();
        const currentMemberId = state.members.ids.find(
          id => state.members.entities[id]?.userId === user?.id
        );
        const affectsCurrentUser = !!currentMemberId && payload.entityAccessIds.some(id => {
          const ea = entityAccessSelectors.selectById(state, id as string);
          return ea?.workspaceMemberId === currentMemberId;
        });
        dispatch(entityAccessSlice.actions.removeMany(payload.entityAccessIds));
        if (affectsCurrentUser) {
          dispatch(hierarchyApi.endpoints.getNodeSpaces.initiate(
            { workspaceId, cursor: null },
            { subscribe: false, forceRefetch: true }
          ));
          const isInsideSpace = /\/workspaces\/[^/]+\/(spaces|folders|tasks)\//.test(locationRef.current.pathname);
          if (isInsideSpace) {
            navigateRef.current({ to: `/workspaces/${workspaceId}` });
          }
        }
      }
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

    const onAccessRevoked = () => {
      // Received on personal group — always affects current user, no store lookup needed
      dispatch(hierarchyApi.endpoints.getNodeSpaces.initiate(
        { workspaceId, cursor: null },
        { subscribe: false, forceRefetch: true }
      ));
      const isInsideSpace = /\/workspaces\/[^/]+\/(spaces|folders|tasks)\//.test(locationRef.current.pathname);
      if (isInsideSpace) navigateRef.current({ to: `/workspaces/${workspaceId}` });
    };

    const onAccessGranted = () => {
      // Received on personal group — always affects current user
      dispatch(hierarchyApi.endpoints.getNodeSpaces.initiate(
        { workspaceId, cursor: null },
        { subscribe: false, forceRefetch: true }
      ));
    };

    signalRService.on("EntitiesUpdated", onEntitiesUpdated);
    signalRService.on("EntitiesDeleted", onEntitiesDeleted);
    signalRService.on("AccessRevoked", onAccessRevoked);
    signalRService.on("AccessGranted", onAccessGranted);
    signalRService.onReconnected(handleReconnect);

    return () => {
      signalRService.off("EntitiesUpdated", onEntitiesUpdated);
      signalRService.off("EntitiesDeleted", onEntitiesDeleted);
      signalRService.off("AccessRevoked", onAccessRevoked);
      signalRService.off("AccessGranted", onAccessGranted);
      signalRService.offReconnected(handleReconnect);
      signalRService.invoke("LeaveWorkspace", workspaceId).catch(() => {});
    };
  }, [workspaceId, dispatch]);
}
