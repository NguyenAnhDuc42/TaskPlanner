import {
  createRootRouteWithContext,
  Outlet,
  Scripts,
} from "@tanstack/react-router";
import { TanStackRouterDevtools } from "@tanstack/react-router-devtools";
import type { AuthContextType } from "@/features/auth/auth-context";

import "../index.css";

import { Toaster } from "@/components/ui/sonner";
import { useNotificationSignalR } from "@/features/notifications/use-notification-signalr";

interface RouterContext {
  auth: AuthContextType;
}

const RootLayout = () => {
  useNotificationSignalR();
  return (
    <>
      <Outlet />
      <Toaster/>
      <TanStackRouterDevtools position="bottom-right" />
      <Scripts />
    </>
  );
};

export const Route = createRootRouteWithContext<RouterContext>()({
  component: RootLayout,
});
