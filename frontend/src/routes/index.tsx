import { userQueryOptions } from "@/features/auth/api";
import { createFileRoute, redirect } from "@tanstack/react-router";
import { WorkspaceHomeScreen } from "@/features/main/home-screen";
import { workspaceInfiniteQueryOptions } from "@/features/main/home-screen/api";

export const Route = createFileRoute("/")({
  beforeLoad: async ({ context }) => {
    const user = await context.queryClient.ensureQueryData(userQueryOptions);
    if (!user) {
      throw redirect({ to: "/auth/sign-in" });
    }
  },
  loader: async ({ context }) => {
    await context.queryClient.ensureInfiniteQueryData(
      workspaceInfiniteQueryOptions(),
    );
  },
  component: WorkspaceHomeScreen,
});
