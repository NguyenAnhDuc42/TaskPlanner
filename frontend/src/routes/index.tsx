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

    const lastWorkspaceId = typeof window !== "undefined" ? localStorage.getItem("lastWorkspaceId") : null;

    if (lastWorkspaceId && !search.select) {
      const attempts = parseInt(sessionStorage.getItem("lastWorkspaceRedirectAttempts") ?? "0");

      if (attempts >= 2) {
        localStorage.removeItem("lastWorkspaceId");
        sessionStorage.removeItem("lastWorkspaceRedirectAttempts");
      } else {
        sessionStorage.setItem("lastWorkspaceRedirectAttempts", String(attempts + 1));
        throw redirect({
          to: "/workspaces/$workspaceId",
          params: { workspaceId: lastWorkspaceId },
        });
      }
    } else {
      sessionStorage.removeItem("lastWorkspaceRedirectAttempts");
    }
  },
  loaderDeps: ({ search }) => search,
  component: WorkspaceHomeScreen,
});
