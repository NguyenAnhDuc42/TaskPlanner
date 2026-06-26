import { createFileRoute } from "@tanstack/react-router";
import { workspaceSearchSchema } from "../workspace-search-schema";
import { ComingSoon } from "@/components/coming-soon";
import { ViewSkeleton } from "@/components/view-skeleton";

export const Route = createFileRoute("/workspaces/$workspaceId/")({
  validateSearch: (search) => workspaceSearchSchema.parse(search),
  pendingComponent: ViewSkeleton,
  component: () => (
    <ComingSoon
      title="Command Center"
      description="A keyboard-first power hub for searching, navigating, and running commands across your entire workspace."
    />
  ),
});
