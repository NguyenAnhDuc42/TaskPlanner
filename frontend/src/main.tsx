import { StrictMode } from "react";
import ReactDOM from "react-dom/client";
import { RouterProvider, createRouter } from "@tanstack/react-router";
import { AuthProvider, useAuth } from "./features/auth/auth-context";
import { routeTree } from "./routeTree.gen";

// Create a new router instance
// We pass undefined! for auth because it will be injected by the RouterProvider at runtime
const router = createRouter({
  routeTree: routeTree,
  defaultPreload: "intent",
  defaultPendingMs: 0,
  defaultPendingMinMs: 150,
  context: {
    auth: undefined!,
  },
});

// Register the router instance for type safety
declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}

// eslint-disable-next-line react-refresh/only-export-components
function InnerApp() {
  const auth = useAuth();
  return <RouterProvider router={router} context={{ auth }} />;
}

// Render the app
const rootElement = document.getElementById("root")!;

// Use a cached root to prevent "already been passed to createRoot()" warnings during Vite HMR
declare global {
  interface Window {
    __reactRoot?: ReactDOM.Root;
  }
}

let root = window.__reactRoot;
if (!root) {
  root = ReactDOM.createRoot(rootElement);
  window.__reactRoot = root;
}

import { Provider } from "react-redux";
import { store } from "./store";
import { apiEvents } from "./lib/api-client";
import { workspaceApi } from "./store/workspaceApi";

// Wire up the token refresh invalidation for Redux
apiEvents.onTokenRefreshed.push(() => {
  store.dispatch(workspaceApi.util.invalidateTags(["User"]));
});

root.render(
  <StrictMode>
    <Provider store={store}>
      <AuthProvider>
        <InnerApp />
      </AuthProvider>
    </Provider>
  </StrictMode>
);
