import { SignUpPage } from "@/features/auth/sign-up/components";
import { createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/auth/sign-up")({
  component: SignUpPage,
});
