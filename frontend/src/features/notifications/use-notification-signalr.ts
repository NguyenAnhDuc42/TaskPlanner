import { useEffect } from "react";
import { useDispatch } from "react-redux";
import { signalRService } from "@/lib/signalr-service";
import { notificationSlice } from "@/store/entityStore";
import { useUser } from "@/features/auth/auth-api";
import type { AppDispatch } from "@/store";
import type { NotificationRecord } from "@/types/notification-record";

// Global — called from root layout, works on / and everywhere.
// Owns the user SignalR group + listens to the dedicated NewNotification event.
export function useNotificationSignalR() {
  const dispatch = useDispatch<AppDispatch>();
  const { data: currentUser } = useUser();

  useEffect(() => {
    if (!currentUser?.id) return;

    const setup = async () => {
      await signalRService.startConnection();
      await signalRService.invoke("JoinUser", currentUser.id);
    };

    setup().catch((err) => console.error("[NotificationSignalR]", err));

    const onNewNotification = (record: NotificationRecord) => {
      dispatch(notificationSlice.actions.upsert(record));
    };

    signalRService.on("NewNotification", onNewNotification);

    return () => {
      signalRService.off("NewNotification", onNewNotification);
    };
  }, [currentUser?.id, dispatch]);
}
