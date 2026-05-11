import { StrictMode } from "react";
import ReactDOM from "react-dom/client";
import { RouterProvider, createRouter } from "@tanstack/react-router";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AuthProvider, useAuth } from "./features/auth/auth-context";
import { routeTree } from "./routeTree.gen";

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60, // 1 minute
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
});

// Create a new router instance
// We pass undefined! for auth because it will be injected by the RouterProvider at runtime
const router = createRouter({
  routeTree: routeTree,
  context: {
    queryClient,
    auth: undefined!,
  },
});

// Register the router instance for type safety
declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}

function InnerApp() {
  const auth = useAuth();
  return <RouterProvider router={router} context={{ auth, queryClient }} />;
}

// Render the app
const rootElement = document.getElementById("root")!;

// Use a cached root to prevent "already been passed to createRoot()" warnings during Vite HMR
let root = (window as any).__reactRoot;
if (!root) {
  root = ReactDOM.createRoot(rootElement);
  (window as any).__reactRoot = root;
}

root.render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <InnerApp />
      </AuthProvider>
    </QueryClientProvider>
  </StrictMode>
);
