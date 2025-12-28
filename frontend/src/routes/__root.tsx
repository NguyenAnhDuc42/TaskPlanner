import { createRootRoute, Outlet, Scripts } from "@tanstack/react-router";
import { TanStackRouterDevtools } from "@tanstack/react-router-devtools";

import "../index.css";

const RootLayout = () => (
  <>
    <Outlet /> {/* Your pages */}
    <TanStackRouterDevtools position="bottom-right" />
    <Scripts /> {/* This injects scripts into the REAL body */}
  </>
);

export const Route = createRootRoute({ component: RootLayout });
