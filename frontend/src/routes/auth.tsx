import { createFileRoute, Outlet, redirect } from "@tanstack/react-router";
import { getCookie } from "@/lib/cookie-utils";

export const Route = createFileRoute("/auth")({
  beforeLoad: async () => {  
    const isLoggedIn = !!getCookie("is_logged_in");

    if (isLoggedIn) {
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
