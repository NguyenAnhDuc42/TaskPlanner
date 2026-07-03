import { useEffect } from "react";
import { useDispatch } from "react-redux";
import { signalRService } from "@/lib/signalr-service";
import { useUser } from "@/features/auth/auth-api";
import { useStore } from "@/stores/root.store";
import { workspaceApi } from "@/store/workspaceApi";
import type { AppDispatch } from "@/store";
import type { NotificationRecord } from "@/types/notification-record";

// Global — called from root layout, works on / and everywhere.
// Owns the user SignalR group + listens to the dedicated NewNotification event.
export function useNotificationSignalR() {
  const dispatch = useDispatch<AppDispatch>();
  const rootStore = useStore();
  const { data: currentUser } = useUser();

  useEffect(() => {
    if (!currentUser?.id) return;

    const setup = async () => {
      await signalRService.startConnection();
      await signalRService.invoke("JoinUser", currentUser.id);
    };

    setup().catch((err) => console.error("[NotificationSignalR]", err));

    const onNewNotification = (record: NotificationRecord) => {
      rootStore.notificationStore.upsert(record);
      rootStore.notificationDB?.put(record).catch((err) =>
        console.error("Failed to persist notification", err),
      );
    };

    // Unrelated to notifications — piggybacks on the same SignalR connection to invalidate the
    // (still Redux/RTK) workspace list cache when the user gets added to a new workspace.
    const onWorkspaceJoined = () => {
      dispatch(workspaceApi.util.invalidateTags(["Workspaces"]));
    };

    signalRService.on("NewNotification", onNewNotification);
    signalRService.on("WorkspaceJoined", onWorkspaceJoined);

    return () => {
      signalRService.off("NewNotification", onNewNotification);
      signalRService.off("WorkspaceJoined", onWorkspaceJoined);
    };
  }, [currentUser?.id, dispatch, rootStore]);
}
