import { useEffect } from "react";
import { useDispatch } from "react-redux";
import { signalRService } from "@/lib/signalr-service";
import { notificationSlice } from "@/store/entityStore";
import { useUser } from "@/features/auth/auth-api";
import type { AppDispatch } from "@/store";
import type { EntityBatchUpdate } from "@/lib/signalr-service";

// Global hook — works on home screen AND workspace.
// Joins the user's personal SignalR group and listens for notification pushes.
export function useNotificationSignalR() {
  const dispatch = useDispatch<AppDispatch>();
  const { data: currentUser } = useUser();

  useEffect(() => {
    if (!currentUser?.id) return;

    const setup = async () => {
      await signalRService.startConnection();
      await signalRService.invoke("JoinUser", currentUser.id);
    };

    setup().catch((err) => console.error("[NotificationSignalR] setup error:", err));

    const onEntitiesUpdated = (payload: EntityBatchUpdate) => {
      if (payload.notifications?.length) {
        dispatch(notificationSlice.actions.upsertMany(payload.notifications));
      }
    };

    signalRService.on("EntitiesUpdated", onEntitiesUpdated);

    return () => {
      signalRService.off("EntitiesUpdated", onEntitiesUpdated);
    };
  }, [currentUser?.id, dispatch]);
}
