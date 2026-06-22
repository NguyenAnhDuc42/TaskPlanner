import { ForgotPasswordPage } from "@/features/auth/forgot-password/components";
import { createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/auth/forgot-password")({
  component: ForgotPasswordPage,
});
