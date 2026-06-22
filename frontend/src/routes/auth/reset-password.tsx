import { ResetPasswordPage } from "@/features/auth/reset-password/components";
import { createFileRoute, redirect } from "@tanstack/react-router";
import { z } from "zod";

export const Route = createFileRoute("/auth/reset-password")({
  validateSearch: z.object({ token: z.string().min(1) }),
  beforeLoad: ({ search }) => {
    if (!search.token) {
      throw redirect({ to: "/auth/forgot-password" });
    }
  },
  component: ResetPasswordRoute,
});

function ResetPasswordRoute() {
  const { token } = Route.useSearch();
  return <ResetPasswordPage token={token} />;
}
