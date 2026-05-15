import {
  createRootRouteWithContext,
  Outlet,
  Scripts,
} from "@tanstack/react-router";
import { TanStackRouterDevtools } from "@tanstack/react-router-devtools";
import type { AuthContextType } from "@/features/auth/auth-context";

import "../index.css";

import type { QueryClient } from "@tanstack/react-query";
import { Toaster } from "@/components/ui/sonner";

interface RouterContext {
  auth: AuthContextType;
  queryClient: QueryClient;
}

const RootLayout = () => (
  <>
    <Outlet />
    <Toaster/>
    <TanStackRouterDevtools position="bottom-right" />
    <Scripts /> {/* This injects scripts into the REAL body */}
  </>
);

export const Route = createRootRouteWithContext<RouterContext>()({
  component: RootLayout,
});
