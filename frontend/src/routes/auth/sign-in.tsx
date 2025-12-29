import { SignInPage } from "@/features/auth/sign-in/components";
import { createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/auth/sign-in")({
  component: SignInPage,
});
