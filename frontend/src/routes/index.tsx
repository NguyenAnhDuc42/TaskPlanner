import { userQueryOptions } from "@/features/auth/api";
import { createFileRoute, redirect } from "@tanstack/react-router";
import { WorkspaceHomeScreen } from "@/features/workspace/home-screen";

export const Route = createFileRoute("/")({
  beforeLoad: async ({ context }) => {
    const user = await context.queryClient.ensureQueryData(userQueryOptions);
    if (!user) {
      throw redirect({ to: "/auth/sign-in" });
    }
  },
  component: WorkspaceHomeScreen,
});
