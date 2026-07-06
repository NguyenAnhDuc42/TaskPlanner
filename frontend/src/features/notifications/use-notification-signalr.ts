import { useEffect } from "react";
import { signalRService } from "@/lib/signalr-service";
import { useUser } from "@/features/auth/auth-api";
import { useStore } from "@/stores/root.store";
import { apiEvents } from "@/lib/api-client";
import type { NotificationRecord } from "@/types/notification-record";

// Global — called from root layout, works on / and everywhere.
// Owns the user SignalR group + listens to the dedicated NewNotification event.
export function useNotificationSignalR() {
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

    signalRService.on("NewNotification", onNewNotification);

    const onWorkspaceAccessRevoked = ({ workspaceId }: { workspaceId: string }) => {
      apiEvents.onWorkspaceAccessRevoked.forEach((cb) => cb(workspaceId));
    };
    signalRService.on("WorkspaceAccessRevoked", onWorkspaceAccessRevoked);

    return () => {
      signalRService.off("NewNotification", onNewNotification);
      signalRService.off("WorkspaceAccessRevoked", onWorkspaceAccessRevoked);
    };
  }, [currentUser?.id, rootStore]);
}
