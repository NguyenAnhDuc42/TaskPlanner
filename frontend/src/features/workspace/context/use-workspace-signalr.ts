import { useEffect, useRef, useMemo, useLayoutEffect } from "react";
import { useUser } from "@/features/auth/auth-api";
import { useStore } from "@/stores/root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { WorkspaceMutations } from "@/mutations/workspace.mutations";
import { signalRService } from "@/lib/signalr-service";


export function useWorkspaceSignalR(workspaceId: string) {
  const rootStore = useStore();
  const syncEngine = useSyncEngine();
  const workspaceMutations = useMemo(() => new WorkspaceMutations(rootStore, syncEngine), [rootStore, syncEngine]);
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

    const handleReconnect = () => {
      console.log("[SignalR] Reconnected. Syncing active screen views...");
      signalRService.invoke("JoinWorkspace", workspaceId).catch(() => {});
      const userId = currentUserRef.current?.id;
      if (userId) signalRService.invoke("JoinUser", userId).catch(() => {});
      workspaceMutations.fetchDetail(workspaceId).catch((err) => console.error("Failed to refresh workspace permissions", err));
    };

    signalRService.onReconnected(handleReconnect);
    const unregisterVisibility = signalRService.registerVisibilityReconnect();

    return () => {
      signalRService.offReconnected(handleReconnect);
      unregisterVisibility();
      signalRService.invoke("LeaveWorkspace", workspaceId).catch(() => {});
    };
  }, [workspaceId, workspaceMutations]);
}
