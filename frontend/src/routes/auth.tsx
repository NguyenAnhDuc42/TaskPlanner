import { createFileRoute, Outlet, redirect } from "@tanstack/react-router";
import { userQueryOptions } from "@/features/auth/api";

export const Route = createFileRoute("/auth")({
  beforeLoad: async ({ context }) => {  
    const user = await context.queryClient.ensureQueryData(userQueryOptions);

    if (user) {
      throw redirect({
        to: "/",
      });
    }
  },
  component: AuthLayout,
});

function AuthLayout() {
  return (
    <div className="auth-layout">
      <Outlet />
    </div>
  );
}
