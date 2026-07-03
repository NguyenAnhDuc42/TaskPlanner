import {
  createRootRouteWithContext,
  Outlet,
  Scripts,
} from "@tanstack/react-router";
import { useEffect, useMemo } from "react";
import { TanStackRouterDevtools } from "@tanstack/react-router-devtools";
import type { AuthContextType } from "@/features/auth/auth-context";

import "../index.css";

import { Toaster } from "@/components/ui/sonner";
import { useNotificationSignalR } from "@/features/notifications/use-notification-signalr";
import { useUser } from "@/features/auth/auth-api";
import { RootStore, RootStoreProvider } from "@/stores/root.store";
import { NotificationMutations } from "@/mutations/notification.mutations";
import { devError } from "@/sync/dev-log";

interface RouterContext {
  auth: AuthContextType;
}

// RootStore lives here, above all routing — not inside SyncProvider (workspace-scoped) — because
// user-level state (notificationStore, workspaceStore) needs to exist both inside a workspace and
// on the pre-workspace home screen. SyncProvider now calls switchWorkspace()/init() on this same
// shared instance instead of constructing its own.
function AppShell() {
  const rootStore = useMemo(() => new RootStore(), []);
  const { data: currentUser } = useUser();

  useEffect(() => {
    if (!currentUser?.id) return;
    let cancelled = false;

    (async () => {
      try {
        await rootStore.initUser(currentUser.id);
        if (cancelled) return;
        await new NotificationMutations(rootStore).fetchInitial(50);
      } catch (err) {
        devError("[AppShell] failed to init user store:", err);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [currentUser?.id, rootStore]);

  return (
    <RootStoreProvider value={rootStore}>
      <NotificationBridge />
      <Outlet />
      <Toaster />
      <TanStackRouterDevtools position="bottom-right" />
      <Scripts />
    </RootStoreProvider>
  );
}

// Split out so useNotificationSignalR() (which reads rootStore via useStore()) runs inside the
// RootStoreProvider tree above.
function NotificationBridge() {
  useNotificationSignalR();
  return null;
}

export const Route = createRootRouteWithContext<RouterContext>()({
  component: AppShell,
});
