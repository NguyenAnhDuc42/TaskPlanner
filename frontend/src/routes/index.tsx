import { userQueryOptions } from "@/features/auth/api";
import { createFileRoute, redirect } from "@tanstack/react-router";
import { WorkspaceHomeScreen } from "@/features/main/home-screen";
import { workspaceInfiniteQueryOptions } from "@/features/main/home-screen/api";
import { userPreferenceQueryOptions } from "@/features/main/user-preference-api";
import { z } from "zod";

const workspaceSearchSchema = z.object({
  name: z.string().optional(),
  variant: z.string().optional(),
  owned: z.boolean().optional(),
  isArchived: z.boolean().optional(),
  direction: z.enum(["Ascending", "Descending"]).optional(),
});

export type WorkspaceSearch = z.infer<typeof workspaceSearchSchema>;

export const Route = createFileRoute("/")({
  validateSearch: (search) => workspaceSearchSchema.parse(search),
  beforeLoad: async ({ context }) => {
    const user = await context.queryClient.ensureQueryData(userQueryOptions);
    if (!user) {
      throw redirect({ to: "/auth/sign-in" });
    }

    // Try to redirect to last active workspace
    try {
      const preferences = await context.queryClient.ensureQueryData(
        userPreferenceQueryOptions,
      );

      if (preferences?.lastWorkspaceId) {
        throw redirect({
          to: "/workspaces/$workspaceId",
          params: { workspaceId: preferences.lastWorkspaceId },
        });
      }
    } catch (e: any) {
      // If it's a redirect (from TanStack Router), re-throw it
      if (e?.isRedirect || e?.to) throw e;
      // Otherwise preferences fetch failed — continue to home screen
      console.warn("[Index] Failed to fetch preferences, showing home screen");
    }
  },
  loaderDeps: ({ search }) => search,
  loader: async ({ context, deps }) => {
    await context.queryClient.ensureInfiniteQueryData(
      workspaceInfiniteQueryOptions(deps as WorkspaceSearch),
    );
  },
  component: WorkspaceHomeScreen,
});
