import { createFileRoute, Outlet, redirect } from "@tanstack/react-router";

export const Route = createFileRoute("/auth/")({
  beforeLoad: async ({ context }) => {
    if (context.auth.isAuthenticated) {
      throw redirect({
        to: "/",
      });
    }
  },
  component: AuthLayout,
});

function AuthLayout() {
  return <Outlet />;
}
