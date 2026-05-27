import { createFileRoute, redirect } from "@tanstack/react-router";
import { WorkspaceHomeScreen } from "@/features/main/home-screen";
import { z } from "zod";
import { getCookie } from "@/lib/cookie-utils";

const workspaceSearchSchema = z.object({
  name: z.string().optional(),
  variant: z.string().optional(),
  owned: z.boolean().optional(),
  isArchived: z.boolean().optional(),
  direction: z.enum(["Ascending", "Descending"]).optional(),
  select: z.boolean().optional(),
});

export type WorkspaceSearch = z.infer<typeof workspaceSearchSchema>;

export const Route = createFileRoute("/")({
  validateSearch: (search) => workspaceSearchSchema.parse(search),
  beforeLoad: async ({ search }) => {
    const isLoggedIn = !!getCookie("is_logged_in");
    if (!isLoggedIn) {
      throw redirect({ to: "/auth/sign-in" });
    }

    // Try to redirect to last active workspace from LocalStorage
    const lastWorkspaceId = typeof window !== "undefined" ? localStorage.getItem("lastWorkspaceId") : null;

    // Only redirect if select is NOT true
    if (lastWorkspaceId && !search.select) {
      throw redirect({
        to: "/workspaces/$workspaceId",
        params: { workspaceId: lastWorkspaceId },
      });
    }
  },
  loaderDeps: ({ search }) => search,
  component: WorkspaceHomeScreen,
});
